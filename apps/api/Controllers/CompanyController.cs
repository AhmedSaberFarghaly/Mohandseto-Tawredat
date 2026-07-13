using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Controllers;

[ApiController]
[Route("api/company")]
[Authorize]
public class CompanyController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];
    private static readonly string[] AllowedContentTypes = ["application/pdf", "image/jpeg", "image/png"];
    private const long MaxFileBytes = 10 * 1024 * 1024;

    private Guid TenantId => Guid.TryParse(User.FindFirstValue("tenant_id"), out var id)
        ? id
        : throw ApiException.Forbidden("هذا الحساب غير مرتبط بشركة");

    /// <summary>Verification status screen (screens 32-38): tenant status + per-document review state.</summary>
    [HttpGet("verification-status")]
    public async Task<IActionResult> VerificationStatus(CancellationToken ct)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == TenantId, ct)
            ?? throw ApiException.NotFound();
        var docs = await db.CompanyDocuments
            .Select(d => new { d.Id, d.Type, d.FileName, d.ReviewStatus, d.RejectionReason, d.CreatedAt })
            .ToListAsync(ct);
        return Ok(new { tenantStatus = tenant.Status.ToString(), documents = docs });
    }

    /// <summary>Upload/replace a verification document (screens 27-31: سجل تجاري، بطاقة ضريبية، خطاب تفويض).</summary>
    [HttpPost("documents")]
    [RequestSizeLimit(MaxFileBytes + 1024)]
    public async Task<IActionResult> UploadDocument([FromForm] string type, IFormFile file, CancellationToken ct)
    {
        if (!Enum.TryParse<CompanyDocumentType>(type, ignoreCase: true, out var docType))
            throw ApiException.BadRequest("نوع المستند غير صحيح");
        if (file.Length is 0 or > MaxFileBytes)
            throw ApiException.BadRequest("حجم الملف يجب أن يكون بين 1 بايت و10 ميجابايت");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext) || !AllowedContentTypes.Contains(file.ContentType))
            throw ApiException.BadRequest("صيغة الملف غير مدعومة — المسموح: PDF أو JPG أو PNG");

        var company = await db.Companies.FirstOrDefaultAsync(c => c.TenantId == TenantId, ct)
            ?? throw ApiException.NotFound("لا توجد شركة مرتبطة بالحساب");

        // tenant-scoped storage path outside wwwroot; served only through authorized endpoints
        var dir = Path.Combine(env.ContentRootPath, "storage", "tenants", TenantId.ToString(), "docs");
        Directory.CreateDirectory(dir);
        var storedName = $"{docType}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, storedName);
        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream, ct);

        // re-uploading a rejected/pending document replaces it
        var existing = await db.CompanyDocuments.FirstOrDefaultAsync(d => d.Type == docType, ct);
        if (existing is not null) db.CompanyDocuments.Remove(existing);

        var doc = new CompanyDocument
        {
            TenantId = TenantId,
            CompanyId = company.Id,
            Type = docType,
            FileName = file.FileName,
            StoragePath = Path.Combine("storage", "tenants", TenantId.ToString(), "docs", storedName),
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            ReviewStatus = DocumentReviewStatus.Pending,
        };
        db.CompanyDocuments.Add(doc);

        // once all three required documents exist, the tenant moves to UnderReview
        var tenant = await db.Tenants.FirstAsync(t => t.Id == TenantId, ct);
        if (tenant.Status == TenantStatus.PendingVerification)
        {
            var typesAfterSave = await db.CompanyDocuments
                .Where(d => d.Id != (existing == null ? Guid.Empty : existing.Id))
                .Select(d => d.Type).ToListAsync(ct);
            typesAfterSave.Add(docType);
            var required = new[]
            {
                CompanyDocumentType.CommercialRegistration,
                CompanyDocumentType.TaxCard,
                CompanyDocumentType.AuthorizationLetter,
            };
            if (required.All(typesAfterSave.Contains))
                tenant.Status = TenantStatus.UnderReview;
        }

        db.AuditLogs.Add(new AuditLog
        {
            TenantId = TenantId,
            Action = "company.document_uploaded",
            EntityType = nameof(CompanyDocument),
            EntityId = doc.Id.ToString(),
            DataJson = $"{{\"type\":\"{docType}\"}}",
        });
        await db.SaveChangesAsync(ct);

        return Ok(new { doc.Id, type = docType.ToString(), status = doc.ReviewStatus.ToString() });
    }

    [HttpGet("documents/{id:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id, CancellationToken ct)
    {
        var doc = await db.CompanyDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id && d.TenantId == TenantId, ct)
            ?? throw ApiException.NotFound("المستند غير موجود");
        var root = Path.GetFullPath(env.ContentRootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, doc.StoragePath));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
            throw ApiException.NotFound("ملف المستند غير موجود");
        return PhysicalFile(fullPath, doc.ContentType, doc.FileName, enableRangeProcessing: true);
    }
}

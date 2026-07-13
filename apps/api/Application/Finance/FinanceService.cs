using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Finance;

public sealed class FinanceService(AppDbContext db, ITenantProvider tenantProvider, IWebHostEnvironment environment,
    IConfiguration configuration)
{
    private static readonly HashSet<string> ReceiptTypes = ["image/jpeg", "image/png", "application/pdf"];

    public Invoice IssueForOrder(Order order)
    {
        var issued = DateTime.UtcNow; var invoice = new Invoice { TenantId = order.TenantId, OrderId = order.Id,
            UserId = order.UserId, Number = $"INV-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            Status = order.Status == OrderStatus.PendingApproval ? InvoiceStatus.Draft : InvoiceStatus.Issued,
            IssuedAt = issued, DueAt = issued.AddDays(30), SellerTaxNumber = configuration["Company:TaxNumber"] ?? "EG-MOHANDSETO-TAX",
            Subtotal = order.Subtotal, Discount = order.Savings + order.CouponDiscount, Tax = order.TaxIncluded,
            Shipping = order.Shipping, Total = order.Total };
        foreach (var item in order.Items) invoice.Lines.Add(new InvoiceLine { TenantId = order.TenantId, OrderItemId = item.Id,
            Sku = item.Sku, DescriptionAr = item.NameAr, Quantity = item.Quantity, UnitPrice = item.UnitPrice,
            TaxAmount = order.Subtotal <= 0 ? 0 : decimal.Round(order.TaxIncluded * item.LineTotal / order.Subtotal, 2), LineTotal = item.LineTotal });
        db.Invoices.Add(invoice); return invoice;
    }

    public async Task<List<InvoiceListDto>> ListAsync(string? status, string? search, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        await RefreshOverdueAsync(ct); var query = db.Invoices.AsNoTracking().Include(i => i.Order).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvoiceStatus>(status, true, out var parsed)) query = query.Where(i => i.Status == parsed);
        if (!string.IsNullOrWhiteSpace(search)) { var value = search.Trim(); query = query.Where(i => i.Number.Contains(value) || i.Order.Number.Contains(value)); }
        if (from is not null) query = query.Where(i => i.IssuedAt >= from.Value.Date); if (to is not null) query = query.Where(i => i.IssuedAt < to.Value.Date.AddDays(1));
        return await query.OrderByDescending(i => i.IssuedAt).Select(i => ListMap(i)).ToListAsync(ct);
    }

    public async Task<InvoiceDetailDto> DetailAsync(Guid id, CancellationToken ct = default)
    {
        await RefreshOverdueAsync(ct); var invoice = await db.Invoices.AsNoTracking().Include(i => i.Order).Include(i => i.Lines).Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id, ct) ?? throw ApiException.NotFound("الفاتورة غير موجودة"); return DetailMap(invoice);
    }

    public async Task<FinanceSummaryDto> SummaryAsync(CancellationToken ct = default)
    {
        await RefreshOverdueAsync(ct); var invoices = await db.Invoices.AsNoTracking().Include(i => i.Order)
            .Where(i => i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft).ToListAsync(ct);
        var payments = await db.InvoicePayments.AsNoTracking().Where(p => p.Status == InvoicePaymentStatus.Completed)
            .OrderByDescending(p => p.VerifiedAt).Take(10).ToListAsync(ct);
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.TenantId == TenantId(), ct); var limit = company?.CreditLimit ?? 0;
        var outstanding = invoices.Sum(i => Math.Max(0, i.Total - i.PaidAmount)); var used = Math.Min(limit, outstanding);
        return new(outstanding, invoices.Where(i => i.Status == InvoiceStatus.Overdue).Sum(i => i.Total - i.PaidAmount),
            invoices.Where(i => i.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid && i.DueAt <= DateTime.UtcNow.AddDays(7)).Sum(i => i.Total - i.PaidAmount),
            invoices.Count(i => i.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid or InvoiceStatus.Overdue), limit, used, Math.Max(0, limit - used),
            limit <= 0 ? 0 : decimal.Round(used / limit * 100, 1), invoices.Where(i => i.Total > i.PaidAmount).OrderBy(i => i.DueAt).Take(8).Select(ListMap).ToList(), payments.Select(PaymentMap).ToList());
    }

    public async Task<InvoicePaymentStartedDto> StartPaymentAsync(Guid userId, Guid invoiceId, StartInvoicePaymentDto dto, CancellationToken ct = default)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct) ?? throw ApiException.NotFound("الفاتورة غير موجودة");
        var outstanding = invoice.Total - invoice.PaidAmount; if (dto.Amount <= 0 || dto.Amount > outstanding) throw ApiException.BadRequest("قيمة التحويل تتجاوز المبلغ المستحق");
        var payment = new InvoicePayment { TenantId = invoice.TenantId, InvoiceId = invoice.Id, UserId = userId, Amount = dto.Amount,
            Reference = $"PAY-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}", BankReference = Clean(dto.BankReference, 100) };
        db.InvoicePayments.Add(payment); await db.SaveChangesAsync(ct); return new(payment.Id, payment.Reference, payment.Amount, payment.Status.ToString(),
            configuration["Payments:BankName"] ?? "CIB", configuration["Payments:BankAccountName"] ?? "Mohandseto Tawredat",
            configuration["Payments:BankIban"] ?? "EG00 0000 0000 0000 0000 0000 000");
    }

    public async Task<InvoicePaymentDto> UploadReceiptAsync(Guid userId, Guid paymentId, IFormFile file, CancellationToken ct = default)
    {
        var payment = await db.InvoicePayments.Include(p => p.Invoice).FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId, ct) ?? throw ApiException.NotFound("عملية الدفع غير موجودة");
        if (payment.Status is not (InvoicePaymentStatus.Initiated or InvoicePaymentStatus.Rejected)) throw ApiException.Conflict("لا يمكن تحديث إيصال هذه العملية");
        if (file.Length is <= 0 or > 10 * 1024 * 1024 || !ReceiptTypes.Contains(file.ContentType)) throw ApiException.BadRequest("الإيصال يجب أن يكون صورة أو PDF وبحجم أقصى 10MB");
        var ext = file.ContentType switch { "image/png" => ".png", "image/jpeg" => ".jpg", _ => ".pdf" };
        var folder = Path.Combine(environment.ContentRootPath, "App_Data", "finance", payment.TenantId.ToString("N"), payment.Id.ToString("N")); Directory.CreateDirectory(folder);
        var absolute = Path.Combine(folder, $"{Guid.NewGuid():N}{ext}"); await using (var stream = File.Create(absolute)) await file.CopyToAsync(stream, ct);
        payment.ReceiptStoredPath = Path.GetRelativePath(environment.ContentRootPath, absolute).Replace('\\', '/'); payment.ReceiptOriginalName = Path.GetFileName(file.FileName);
        payment.ReceiptContentType = file.ContentType; payment.Status = InvoicePaymentStatus.PendingVerification; payment.RejectionReason = null;
        await db.SaveChangesAsync(ct); return PaymentMap(payment);
    }

    public async Task<InvoicePaymentDto> DecidePaymentAsync(Guid staffId, Guid paymentId, PaymentDecisionDto dto, CancellationToken ct = default)
    {
        var payment = await db.InvoicePayments.IgnoreQueryFilters().Include(p => p.Invoice).FirstOrDefaultAsync(p => p.Id == paymentId && !p.IsDeleted, ct) ?? throw ApiException.NotFound("عملية الدفع غير موجودة");
        if (payment.Status != InvoicePaymentStatus.PendingVerification) throw ApiException.Conflict("عملية الدفع ليست قيد التحقق");
        if (!dto.Approved) { payment.Status = InvoicePaymentStatus.Rejected; payment.RejectionReason = Clean(dto.Note, 500) ?? "تعذر التحقق من الإيصال"; }
        else
        {
            payment.Status = InvoicePaymentStatus.Completed; payment.VerifiedAt = DateTime.UtcNow; payment.VerifiedBy = staffId; payment.Invoice.PaidAmount += payment.Amount;
            payment.Invoice.Status = payment.Invoice.PaidAmount >= payment.Invoice.Total ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        }
        db.Notifications.Add(new AppNotification { TenantId = payment.TenantId, UserId = payment.UserId, Type = "invoice.payment", Title = dto.Approved ? "تم تأكيد الدفعة" : "تعذر تأكيد الدفعة",
            Body = $"{payment.Reference}: {payment.Amount:0.##} ج.م", EntityType = nameof(Invoice), EntityId = payment.InvoiceId }); await db.SaveChangesAsync(ct); return PaymentMap(payment);
    }

    public async Task<CreditLimitRequestResultDto> RequestCreditAsync(Guid userId, CreditLimitRequestDto dto, CancellationToken ct = default)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.TenantId == TenantId(), ct) ?? throw ApiException.NotFound("بيانات الشركة غير موجودة");
        if (dto.RequestedLimit <= company.CreditLimit || string.IsNullOrWhiteSpace(dto.Reason)) throw ApiException.BadRequest("الحد المطلوب يجب أن يزيد عن الحد الحالي مع توضيح السبب");
        if (await db.CreditLimitRequests.AnyAsync(r => r.Status == CreditLimitRequestStatus.Submitted || r.Status == CreditLimitRequestStatus.UnderReview, ct)) throw ApiException.Conflict("يوجد طلب حد ائتماني قيد المراجعة");
        var request = new CreditLimitRequest { TenantId = TenantId(), UserId = userId, CurrentLimit = company.CreditLimit, RequestedLimit = dto.RequestedLimit, Reason = Clean(dto.Reason, 1500)! };
        db.CreditLimitRequests.Add(request); await db.SaveChangesAsync(ct); return CreditMap(request);
    }

    public async Task<CreditLimitRequestResultDto> DecideCreditAsync(Guid staffId, Guid id, CreditDecisionDto dto, CancellationToken ct = default)
    {
        var request = await db.CreditLimitRequests.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct) ?? throw ApiException.NotFound("طلب الحد الائتماني غير موجود");
        if (request.Status is CreditLimitRequestStatus.Approved or CreditLimitRequestStatus.Rejected) throw ApiException.Conflict("تم اتخاذ قرار في الطلب بالفعل");
        if (dto.Approved)
        {
            var limit = dto.ApprovedLimit ?? request.RequestedLimit; if (limit <= request.CurrentLimit) throw ApiException.BadRequest("الحد المعتمد غير صالح");
            var company = await db.Companies.IgnoreQueryFilters().FirstAsync(c => c.TenantId == request.TenantId && !c.IsDeleted, ct); company.CreditLimit = limit; request.Status = CreditLimitRequestStatus.Approved;
        }
        else request.Status = CreditLimitRequestStatus.Rejected;
        request.DecisionNote = Clean(dto.Note, 500); request.DecidedAt = DateTime.UtcNow; request.DecidedBy = staffId; await db.SaveChangesAsync(ct); return CreditMap(request);
    }

    public async Task<byte[]> PdfAsync(Guid id, CancellationToken ct = default)
    {
        var d = await DetailAsync(id, ct); var lines = new List<string> { $"TAX INVOICE {d.Number}", $"Order {d.OrderNumber}", $"Issue {d.IssuedAt:yyyy-MM-dd} Due {d.DueAt:yyyy-MM-dd}", $"Tax no {d.SellerTaxNumber}" };
        lines.AddRange(d.Lines.Select(x => $"{x.Sku} | {x.Quantity} x {x.UnitPrice:0.00} | {x.LineTotal:0.00}")); lines.Add($"Subtotal {d.Subtotal:0.00} Tax {d.Tax:0.00} Total EGP {d.Total:0.00}"); return SimplePdf(lines);
    }

    public async Task<byte[]> ExportAsync(string? status, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var invoices = await ListAsync(status, null, from, to, ct); using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Write(zip, "[Content_Types].xml", "<?xml version=\"1.0\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Write(zip, "_rels/.rels", "<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Write(zip, "xl/workbook.xml", "<?xml version=\"1.0\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Invoices\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Write(zip, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var rows = new List<string[]> { new[] { "Invoice", "Order", "Status", "Issued", "Due", "Total", "Paid", "Outstanding" } };
            rows.AddRange(invoices.Select(i => new[] { i.Number, i.OrderNumber, i.Status, i.IssuedAt.ToString("yyyy-MM-dd"), i.DueAt.ToString("yyyy-MM-dd"), i.Total.ToString("0.00"), i.PaidAmount.ToString("0.00"), i.Outstanding.ToString("0.00") }));
            var xmlRows = rows.Select((r, index) => $"<row r=\"{index + 1}\">{string.Join("", r.Select((v, c) => $"<c r=\"{Column(c)}{index + 1}\" t=\"inlineStr\"><is><t>{Escape(v)}</t></is></c>"))}</row>");
            Write(zip, "xl/worksheets/sheet1.xml", $"<?xml version=\"1.0\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>{string.Join("", xmlRows)}</sheetData></worksheet>");
        }
        return output.ToArray();
    }

    private async Task RefreshOverdueAsync(CancellationToken ct) { var late = await db.Invoices.Where(i => i.DueAt < DateTime.UtcNow && i.PaidAmount < i.Total && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft).ToListAsync(ct); if (late.Any(i => i.Status != InvoiceStatus.Overdue)) { foreach (var invoice in late) invoice.Status = InvoiceStatus.Overdue; await db.SaveChangesAsync(ct); } }
    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("تعذر تحديد الشركة");
    private static InvoiceListDto ListMap(Invoice i) => new(i.Id, i.Number, i.Order.Number, i.Status.ToString(), i.Type.ToString(), i.Total, i.PaidAmount, Math.Max(0, i.Total - i.PaidAmount), i.IssuedAt, i.DueAt, i.Status == InvoiceStatus.Overdue);
    private static InvoiceDetailDto DetailMap(Invoice i) => new(i.Id, i.Number, i.OrderId, i.Order.Number, i.Status.ToString(), i.Type.ToString(), i.IssuedAt, i.DueAt, i.Currency, i.SellerTaxNumber, i.BuyerTaxNumber, i.EtaUuid,
        i.Subtotal, i.Discount, i.Tax, i.Shipping, i.Total, i.PaidAmount, Math.Max(0, i.Total - i.PaidAmount), i.Lines.Select(x => new InvoiceLineDto(x.Id, x.Sku, x.DescriptionAr, x.Quantity, x.UnitPrice, x.TaxAmount, x.LineTotal)).ToList(), i.Payments.OrderByDescending(p => p.CreatedAt).Select(PaymentMap).ToList(), $"EGS|{i.Number}|{i.SellerTaxNumber}|{i.IssuedAt:O}|{i.Total:0.00}|{i.Tax:0.00}");
    private static InvoicePaymentDto PaymentMap(InvoicePayment p) => new(p.Id, p.Amount, p.Method, p.Status.ToString(), p.Reference, p.BankReference, p.CreatedAt, p.VerifiedAt, p.ReceiptStoredPath is not null, p.RejectionReason);
    private static CreditLimitRequestResultDto CreditMap(CreditLimitRequest r) => new(r.Id, r.CurrentLimit, r.RequestedLimit, r.Status.ToString(), r.CreatedAt, r.DecisionNote);
    private static string? Clean(string? v, int max) { if (string.IsNullOrWhiteSpace(v)) return null; var value = v.Trim(); return value.Length <= max ? value : value[..max]; }
    private static void Write(ZipArchive zip, string name, string content) { var entry = zip.CreateEntry(name); using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)); writer.Write(content); }
    private static string Escape(string value) => new XElement("x", value).Value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    private static string Column(int i) { var value = ""; for (i++; i > 0; i = (i - 1) / 26) value = (char)('A' + (i - 1) % 26) + value; return value; }
    private static byte[] SimplePdf(IEnumerable<string> lines) { var text = string.Join(" ", lines).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)"); var stream = $"BT /F1 9 Tf 36 800 Td ({text}) Tj ET"; var objects = new[] { "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>", "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>", $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" }; var sb = new StringBuilder("%PDF-1.4\n"); var offsets = new List<int> { 0 }; for (var i = 0; i < objects.Length; i++) { offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString())); sb.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); } var xref = Encoding.ASCII.GetByteCount(sb.ToString()); sb.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n"); for (var i = 1; i < offsets.Count; i++) sb.Append($"{offsets[i]:D10} 00000 n \n"); sb.Append($"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); return Encoding.ASCII.GetBytes(sb.ToString()); }
}

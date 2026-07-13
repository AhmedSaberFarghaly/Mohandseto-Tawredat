using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Shopping;

public sealed class PaymentGatewayService(AppDbContext db, ITenantProvider tenantProvider,
    IWebHostEnvironment environment, IConfiguration configuration)
{
    public bool IsAvailable => environment.IsDevelopment() || !string.IsNullOrWhiteSpace(configuration["Payments:Provider"]);

    public async Task<PaymentAttemptDto> CreateAsync(Guid userId, Guid sessionId, string idempotencyKey, decimal amount, CancellationToken ct = default)
    {
        if (!IsAvailable) throw ApiException.Conflict("بوابة الدفع الإلكتروني غير مهيأة");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100 || amount <= 0)
            throw ApiException.BadRequest("بيانات محاولة الدفع غير صالحة");
        var session = await db.CheckoutSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && s.Status != CheckoutStatus.Submitted, ct)
            ?? throw ApiException.NotFound("جلسة الدفع غير موجودة");
        var existing = await db.PaymentAttempts.FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey.Trim(), ct);
        if (existing is not null) return Map(existing);
        var attempt = new PaymentAttempt
        {
            TenantId = tenantProvider.TenantId ?? throw ApiException.Forbidden("الحساب غير مرتبط بشركة"),
            CheckoutSessionId = session.Id, UserId = userId, Provider = environment.IsDevelopment() ? "Development" : configuration["Payments:Provider"]!,
            ProviderReference = $"PAY-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
            IdempotencyKey = idempotencyKey.Trim(), Amount = amount, Status = PaymentAttemptStatus.RequiresAction,
        };
        db.PaymentAttempts.Add(attempt); await db.SaveChangesAsync(ct); return Map(attempt);
    }

    public async Task<PaymentAttemptDto> ConfirmAsync(Guid userId, Guid attemptId, string token, CancellationToken ct = default)
    {
        var attempt = await db.PaymentAttempts.FirstOrDefaultAsync(p => p.Id == attemptId && p.UserId == userId, ct)
            ?? throw ApiException.NotFound("محاولة الدفع غير موجودة");
        if (attempt.Status == PaymentAttemptStatus.Succeeded) return Map(attempt);
        if (!environment.IsDevelopment())
            throw ApiException.Conflict("تأكيد الدفع يتم من Webhook مزود الخدمة في بيئة الإنتاج");
        if (token == "tok_test_success")
        {
            attempt.Status = PaymentAttemptStatus.Succeeded; attempt.ConfirmedAt = DateTime.UtcNow;
        }
        else
        {
            attempt.Status = PaymentAttemptStatus.Failed; attempt.FailureCode = "card_declined";
            attempt.FailureMessage = "تم رفض وسيلة الدفع التجريبية";
        }
        await db.SaveChangesAsync(ct); return Map(attempt);
    }

    public async Task<PaymentAttempt> RequireSucceededAsync(Guid userId, Guid id, decimal amount, CancellationToken ct)
    {
        var attempt = await db.PaymentAttempts.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct)
            ?? throw ApiException.NotFound("محاولة الدفع غير موجودة");
        if (attempt.Status != PaymentAttemptStatus.Succeeded || attempt.Amount != amount)
            throw ApiException.Conflict("عملية الدفع غير مكتملة أو لا تطابق المبلغ المطلوب");
        return attempt;
    }

    private static PaymentAttemptDto Map(PaymentAttempt p) => new(p.Id, p.ProviderReference, p.Status.ToString(),
        p.Amount, p.Currency, p.FailureCode, p.FailureMessage);
}

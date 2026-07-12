using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Auth;

/// <summary>SMS delivery abstraction — real provider plugs in via configuration later.</summary>
public interface ISmsSender
{
    Task SendAsync(string phone, string message, CancellationToken ct = default);
}

/// <summary>Development adapter: logs the message instead of sending a real SMS.</summary>
public class ConsoleSmsSender(ILogger<ConsoleSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string phone, string message, CancellationToken ct = default)
    {
        logger.LogInformation("[DEV-SMS] to {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}

public class OtpService(AppDbContext db, ISmsSender sms, IWebHostEnvironment env)
{
    private const int ExpiryMinutes = 5;
    private const int MaxAttempts = 5;
    private const int MaxRequestsPer10Min = 3;

    /// <summary>Generates and sends an OTP. Returns the code only in Development (for testing).</summary>
    public async Task<string?> RequestAsync(string phone, OtpPurpose purpose, CancellationToken ct = default)
    {
        phone = NormalizePhone(phone);

        var since = DateTime.UtcNow.AddMinutes(-10);
        var recent = await db.OtpCodes.CountAsync(o => o.Phone == phone && o.CreatedAt >= since, ct);
        if (recent >= MaxRequestsPer10Min)
            throw ApiException.TooMany("تم إرسال عدد كبير من الرموز، انتظر 10 دقائق ثم أعد المحاولة");

        // invalidate previous unconsumed codes for the same purpose
        var previous = await db.OtpCodes
            .Where(o => o.Phone == phone && o.Purpose == purpose && !o.Consumed)
            .ToListAsync(ct);
        foreach (var p in previous) p.Consumed = true;

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        db.OtpCodes.Add(new OtpCode
        {
            Phone = phone,
            CodeHash = TokenService.Hash(code),
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ExpiryMinutes),
        });
        await db.SaveChangesAsync(ct);

        await sms.SendAsync(phone, $"رمز التحقق الخاص بك في مهندسيتو توريدات: {code} — صالح لمدة {ExpiryMinutes} دقائق");
        return env.IsDevelopment() ? code : null;
    }

    /// <summary>Validates an OTP and consumes it. Throws on failure.</summary>
    public async Task VerifyAsync(string phone, string code, OtpPurpose purpose, CancellationToken ct = default)
    {
        phone = NormalizePhone(phone);
        var otp = await db.OtpCodes
            .Where(o => o.Phone == phone && o.Purpose == purpose && !o.Consumed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct)
            ?? throw ApiException.BadRequest("لا يوجد رمز تحقق فعال، اطلب رمزًا جديدًا", "otp_not_found");

        if (otp.ExpiresAt < DateTime.UtcNow)
            throw ApiException.BadRequest("انتهت صلاحية الرمز، اطلب رمزًا جديدًا", "otp_expired");

        otp.Attempts++;
        if (otp.Attempts > MaxAttempts)
        {
            otp.Consumed = true;
            await db.SaveChangesAsync(ct);
            throw ApiException.TooMany("تجاوزت الحد الأقصى للمحاولات، اطلب رمزًا جديدًا");
        }

        if (otp.CodeHash != TokenService.Hash(code))
        {
            await db.SaveChangesAsync(ct);
            throw ApiException.BadRequest("رمز التحقق غير صحيح", "otp_invalid");
        }

        otp.Consumed = true;
        await db.SaveChangesAsync(ct);
    }

    public static string NormalizePhone(string phone)
    {
        var p = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (p.StartsWith("01") && p.Length == 11) p = "+2" + p;   // Egyptian local format
        if (p.StartsWith("2") && p.Length == 12) p = "+" + p;
        if (string.IsNullOrWhiteSpace(p) || p.Length < 10)
            throw ApiException.BadRequest("رقم الهاتف غير صحيح", "phone_invalid");
        return p;
    }
}

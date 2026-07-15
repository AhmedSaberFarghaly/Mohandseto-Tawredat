using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Auth;

public class AuthService(AppDbContext db, TokenService tokens, OtpService otp)
{
    /// <summary>Phone + OTP login. If the phone is unknown, signals the client to start company registration.</summary>
    public async Task<AuthResultDto> LoginWithOtpAsync(string phone, string code, CancellationToken ct = default, LoginContext? context = null)
    {
        phone = OtpService.NormalizePhone(phone);
        await otp.VerifyAsync(phone, code, OtpPurpose.Login, ct);

        var user = await db.Users.Include(u => u.Roles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.Phone == phone, ct);

        if (user is null)
            return new AuthResultDto(IsNewUser: true, null, null, null, null, null);

        if (!user.IsActive && (user.SuspendedUntil is null || user.SuspendedUntil > DateTime.UtcNow))
        {
            db.LoginAudits.Add(new LoginAudit { UserId = user.Id, Identifier = phone, Succeeded = false, FailureReason = "account_suspended", IpAddress = context?.IpAddress, UserAgent = context?.UserAgent, Location = context?.Location });
            await db.SaveChangesAsync(ct); EnsureActive(user);
        }
        EnsureActive(user);
        user.PhoneVerified = true;
        db.LoginAudits.Add(new LoginAudit { UserId = user.Id, Identifier = phone, Succeeded = true, IpAddress = context?.IpAddress, UserAgent = context?.UserAgent, Location = context?.Location });
        return await IssueAsync(user, ct, context);
    }

    public async Task<AuthResultDto> LoginWithEmailAsync(string email, string password, CancellationToken ct = default, LoginContext? context = null)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(u => u.Roles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user?.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            db.LoginAudits.Add(new LoginAudit { UserId = user?.Id, Identifier = email, Succeeded = false, FailureReason = "invalid_credentials", IpAddress = context?.IpAddress, UserAgent = context?.UserAgent, Location = context?.Location });
            await db.SaveChangesAsync(ct);
            throw ApiException.Unauthorized("البريد الإلكتروني أو كلمة المرور غير صحيحة");
        }

        if (!user.IsActive && (user.SuspendedUntil is null || user.SuspendedUntil > DateTime.UtcNow))
        {
            db.LoginAudits.Add(new LoginAudit { UserId = user.Id, Identifier = email, Succeeded = false, FailureReason = "account_suspended", IpAddress = context?.IpAddress, UserAgent = context?.UserAgent, Location = context?.Location });
            await db.SaveChangesAsync(ct); EnsureActive(user);
        }
        EnsureActive(user);
        db.LoginAudits.Add(new LoginAudit { UserId = user.Id, Identifier = email, Succeeded = true, IpAddress = context?.IpAddress, UserAgent = context?.UserAgent, Location = context?.Location });
        if (user.TwoFactorEnabled) return await CreateTwoFactorChallengeAsync(user, ct);
        return await IssueAsync(user, ct, context);
    }

    public async Task<AuthResultDto> RegisterCompanyAsync(RegisterCompanyDto dto, CancellationToken ct = default)
    {
        var phone = OtpService.NormalizePhone(dto.Phone);
        await otp.VerifyAsync(phone, dto.OtpCode, OtpPurpose.Registration, ct);

        if (string.IsNullOrWhiteSpace(dto.CompanyLegalName))
            throw ApiException.BadRequest("اسم الشركة مطلوب");
        if (string.IsNullOrWhiteSpace(dto.AdminFullName))
            throw ApiException.BadRequest("اسم المستخدم الإداري مطلوب");
        if (dto.AdminPassword.Length < 8)
            throw ApiException.BadRequest("كلمة المرور يجب ألا تقل عن 8 أحرف");
        if (await db.Users.AnyAsync(u => u.Phone == phone, ct))
            throw ApiException.Conflict("يوجد حساب مسجل بهذا الرقم بالفعل، سجّل الدخول بدلًا من ذلك");

        var email = dto.AdminEmail?.Trim().ToLowerInvariant();
        if (email is not null && await db.Users.AnyAsync(u => u.Email == email, ct))
            throw ApiException.Conflict("يوجد حساب مسجل بهذا البريد الإلكتروني بالفعل");

        var tenant = new Tenant { Name = dto.CompanyLegalName, Status = TenantStatus.PendingVerification };
        var company = new Company
        {
            TenantId = tenant.Id,
            LegalName = dto.CompanyLegalName.Trim(),
            LegalNameEn = dto.CompanyLegalNameEn?.Trim(),
            CommercialRegistrationNo = dto.CommercialRegistrationNo?.Trim(),
            TaxCardNo = dto.TaxCardNo?.Trim(),
            Phone = phone,
            Email = email,
            Governorate = dto.Governorate,
            City = dto.City,
            AddressLine = dto.AddressLine,
            Industry = dto.Industry,
        };
        var branch = new CompanyBranch
        {
            TenantId = tenant.Id,
            CompanyId = company.Id,
            Name = "الفرع الرئيسي",
            Governorate = dto.Governorate,
            City = dto.City,
            AddressLine = dto.AddressLine,
            Phone = phone,
            IsMain = true,
        };
        var ownerRole = await db.Roles.FirstAsync(r => r.Code == "company_owner" && r.TenantId == null, ct);
        var user = new User
        {
            TenantId = tenant.Id,
            FullName = dto.AdminFullName.Trim(),
            Phone = phone,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.AdminPassword),
            PhoneVerified = true,
            DefaultBranchId = branch.Id,
        };
        user.Roles.Add(new UserRole { UserId = user.Id, RoleId = ownerRole.Id });

        var generalCostCenter = new CostCenter
        {
            TenantId = tenant.Id, Code = "CC-GEN", NameAr = "المصروفات العامة",
            BudgetAmount = 250000, ApprovalThreshold = 5000,
        };
        var operationsCostCenter = new CostCenter
        {
            TenantId = tenant.Id, Code = "CC-OPS", NameAr = "التشغيل والمشتريات",
            BudgetAmount = 500000, ApprovalThreshold = 20000,
        };
        var defaultProject = new CompanyProject
        {
            TenantId = tenant.Id, Code = "PRJ-GEN", NameAr = "مشتريات الشركة العامة",
        };
        var welcomeCoupon = new Coupon
        {
            TenantId = tenant.Id, Code = "WELCOME10", NameAr = "خصم الترحيب",
            DiscountType = CouponDiscountType.Percentage, DiscountValue = 10,
            MinimumSubtotal = 500, MaximumDiscount = 1000,
            StartsAt = DateTime.UtcNow.Date, ExpiresAt = DateTime.UtcNow.Date.AddYears(1),
        };
        var approvalPolicy = new ApprovalPolicy
        {
            TenantId = tenant.Id, NameAr = "سلسلة اعتماد المشتريات", MinimumAmount = 0,
        };
        foreach (var level in new[]
        {
            (1, "مدير القسم", (decimal?)5000, 12),
            (2, "مسؤول الميزانية", (decimal?)20000, 12),
            (3, "المدير المالي", (decimal?)null, 24),
        })
        {
            var approvalLevel = new ApprovalLevel { TenantId = tenant.Id, Sequence = level.Item1,
                NameAr = level.Item2, AuthorityLimit = level.Item3, SlaHours = level.Item4 };
            approvalLevel.Assignments.Add(new ApprovalAssignment { TenantId = tenant.Id, UserId = user.Id });
            approvalPolicy.Levels.Add(approvalLevel);
        }
        db.AddRange(tenant, company, branch, user, generalCostCenter, operationsCostCenter, defaultProject, welcomeCoupon, approvalPolicy);
        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            Action = "company.registered",
            EntityType = nameof(Company),
            EntityId = company.Id.ToString(),
        });
        await db.SaveChangesAsync(ct);

        // reload roles for token issuance
        await db.Entry(user).Collection(u => u.Roles).Query().Include(r => r.Role).LoadAsync(ct);
        return await IssueAsync(user, ct);
    }

    public async Task<AuthResultDto> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = TokenService.Hash(refreshToken);
        var stored = await db.RefreshTokens.Include(t => t.User).ThenInclude(u => u.Roles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw ApiException.Unauthorized("جلسة غير صالحة، سجّل الدخول من جديد");

        if (stored.RevokedAt is not null || stored.ExpiresAt < DateTime.UtcNow)
        {
            // reuse of a revoked token → revoke all sessions for the user (token theft mitigation)
            if (stored.RevokedAt is not null)
            {
                var all = await db.RefreshTokens
                    .Where(t => t.UserId == stored.UserId && t.RevokedAt == null).ToListAsync(ct);
                foreach (var t in all) t.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            throw ApiException.Unauthorized("انتهت الجلسة، سجّل الدخول من جديد");
        }

        EnsureActive(stored.User);
        stored.RevokedAt = DateTime.UtcNow; stored.LastSeenAt = DateTime.UtcNow;
        var result = await IssueAsync(stored.User, ct, new LoginContext(stored.IpAddress, stored.UserAgent));
        stored.ReplacedByTokenHash = TokenService.Hash(result.RefreshToken!);
        await db.SaveChangesAsync(ct);
        return result;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = TokenService.Hash(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is { RevokedAt: null }) { stored.RevokedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); }
    }

    public async Task<AuthResultDto> VerifyTwoFactorAsync(TwoFactorLoginDto dto, CancellationToken ct = default, LoginContext? context = null)
    {
        var hash = TokenService.Hash(dto.ChallengeToken);
        var challenge = await db.TwoFactorChallenges.Include(c => c.User).ThenInclude(u => u.Roles).ThenInclude(r => r.Role).FirstOrDefaultAsync(c => c.TokenHash == hash && !c.Consumed, ct)
            ?? throw ApiException.Unauthorized("جلسة التحقق غير صالحة");
        if (challenge.ExpiresAt < DateTime.UtcNow) throw ApiException.Unauthorized("انتهت مهلة التحقق، سجل الدخول من جديد");
        EnsureActive(challenge.User); await otp.VerifyAsync(challenge.User.Phone, dto.Code, OtpPurpose.TwoFactor, ct); challenge.Consumed = true; await db.SaveChangesAsync(ct); return await IssueAsync(challenge.User, ct, context);
    }

    public async Task<PasswordResetRequestResultDto> RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        var expires = DateTime.UtcNow.AddMinutes(10);
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email == email.Trim().ToLowerInvariant() && u.IsActive, ct);

        // Always return the same response shape so this endpoint cannot be used to enumerate accounts.
        if (user is null)
            return new(true, raw, expires, "رقم الهاتف المسجل", null);

        foreach (var old in await db.PasswordResetChallenges
                     .Where(c => c.UserId == user.Id && !c.Consumed).ToListAsync(ct))
            old.Consumed = true;

        var devCode = await otp.RequestAsync(user.Phone, OtpPurpose.PasswordReset, ct);
        db.PasswordResetChallenges.Add(new PasswordResetChallenge
        {
            UserId = user.Id,
            TokenHash = TokenService.Hash(raw),
            ExpiresAt = expires,
        });
        await db.SaveChangesAsync(ct);
        return new(true, raw, expires, "رقم الهاتف المسجل", devCode);
    }

    public async Task ResetPasswordAsync(PasswordResetDto dto, CancellationToken ct = default)
    {
        if (dto.NewPassword.Length < 8)
            throw ApiException.BadRequest("كلمة المرور يجب ألا تقل عن 8 أحرف");

        var hash = TokenService.Hash(dto.ResetToken);
        var challenge = await db.PasswordResetChallenges
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.TokenHash == hash && !c.Consumed, ct)
            ?? throw ApiException.Unauthorized("رابط استعادة كلمة المرور غير صالح");
        if (challenge.ExpiresAt < DateTime.UtcNow)
            throw ApiException.Unauthorized("انتهت مهلة استعادة كلمة المرور");

        await otp.VerifyAsync(challenge.User.Phone, dto.Code, OtpPurpose.PasswordReset, ct);
        challenge.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        challenge.Consumed = true;
        foreach (var session in await db.RefreshTokens
                     .Where(t => t.UserId == challenge.UserId && t.RevokedAt == null).ToListAsync(ct))
            session.RevokedAt = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = challenge.UserId,
            TenantId = challenge.User.TenantId,
            Action = "auth.password_reset",
            EntityType = nameof(User),
            EntityId = challenge.UserId.ToString(),
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<AuthUserDto> MeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.Include(u => u.Roles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw ApiException.Unauthorized();
        return await ToDtoAsync(user, ct);
    }

    private static void EnsureActive(User user)
    {
        if (!user.IsActive && user.SuspendedUntil is { } until && until <= DateTime.UtcNow)
        {
            user.IsActive = true; user.SuspendedAt = null; user.SuspendedUntil = null; user.SuspensionReason = null;
        }
        if (!user.IsActive) throw ApiException.Forbidden("تم تعطيل هذا الحساب، تواصل مع الدعم");
    }

    private async Task<AuthResultDto> IssueAsync(User user, CancellationToken ct, LoginContext? context = null)
    {
        var roles = user.Roles.Select(r => r.Role.Code).ToList();
        var pair = tokens.Issue(user, roles);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenService.Hash(pair.RefreshToken),
            ExpiresAt = pair.RefreshExpiresAt,
            Device = DeviceName(context?.UserAgent), IpAddress = context?.IpAddress, UserAgent = context?.UserAgent,
            LastSeenAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        var dto = await ToDtoAsync(user, ct);
        return new AuthResultDto(false, dto, pair.AccessToken, pair.AccessExpiresAt, pair.RefreshToken, pair.RefreshExpiresAt);
    }

    private static string? DeviceName(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return null;
        var browser = userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase) ? "Edge" : userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase) ? "Chrome" : userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase) ? "Safari" : "Browser";
        var os = userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase) ? "Windows" : userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ? "Android" : userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ? "iPhone" : userAgent.Contains("Mac", StringComparison.OrdinalIgnoreCase) ? "macOS" : "Unknown";
        return $"{os} · {browser}";
    }

    private async Task<AuthResultDto> CreateTwoFactorChallengeAsync(User user, CancellationToken ct)
    {
        foreach (var old in await db.TwoFactorChallenges.Where(c => c.UserId == user.Id && !c.Consumed).ToListAsync(ct)) old.Consumed = true;
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); var expires = DateTime.UtcNow.AddMinutes(5); var devCode = await otp.RequestAsync(user.Phone, OtpPurpose.TwoFactor, ct);
        db.TwoFactorChallenges.Add(new TwoFactorChallenge { UserId = user.Id, TokenHash = TokenService.Hash(raw), ExpiresAt = expires }); await db.SaveChangesAsync(ct);
        return new AuthResultDto(false, null, null, null, null, null, true, raw, expires, devCode);
    }

    private async Task<AuthUserDto> ToDtoAsync(User user, CancellationToken ct)
    {
        string? tenantStatus = null;
        if (user.TenantId is { } tid)
            tenantStatus = (await db.Tenants.FirstOrDefaultAsync(t => t.Id == tid, ct))?.Status.ToString();
        return new AuthUserDto(
            user.Id, user.FullName, user.Phone, user.Email, user.TenantId,
            user.IsPlatformStaff, tenantStatus,
            user.Roles.Select(r => r.Role.Code).ToList());
    }

}

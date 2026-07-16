using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.AdminSystemSettings;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Auth;

public class AuthService(AppDbContext db, TokenService tokens, OtpService otp, AdminSystemSettingsService? systemSettings = null, IExternalIdentityVerifier? external = null)
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

        await EnsureNotLockedAsync(user, ct);

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
        if (await RequiresTwoFactorAsync(user, ct)) return await CreateTwoFactorChallengeAsync(user, ct);
        return await IssueAsync(user, ct, context);
    }

    public IReadOnlyList<ExternalProviderDto> ExternalProviders() => external?.Providers
        .Select(x => new ExternalProviderDto(x.Code, x.DisplayName, x.Enabled)).ToList() ??
        [new("google", "Google", false), new("microsoft", "Microsoft", false)];

    public async Task<ExternalAuthChallengeDto> BeginExternalAsync(string provider, CancellationToken ct = default)
    {
        var verifier = external ?? throw ApiException.BadRequest("تسجيل الدخول الخارجي غير مهيأ");
        var options = verifier.Provider(provider);
        if (!options.Enabled) throw ApiException.BadRequest($"تسجيل الدخول عبر {options.DisplayName} غير مهيأ بعد");
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var expires = DateTime.UtcNow.AddMinutes(5);
        db.ExternalAuthChallenges.Add(new ExternalAuthChallenge
        {
            Provider = options.Code,
            TokenHash = TokenService.Hash(raw),
            ExpiresAt = expires,
        });
        await db.SaveChangesAsync(ct);
        return new(options.Code, options.ClientId, options.DiscoveryUrl, options.RedirectUrl, options.Scopes, raw, expires, options.IosClientId);
    }

    public async Task<AuthResultDto> LoginExternalAsync(ExternalLoginDto dto, CancellationToken ct = default, LoginContext? context = null)
    {
        var verifier = external ?? throw ApiException.BadRequest("تسجيل الدخول الخارجي غير مهيأ");
        var provider = verifier.Provider(dto.Provider);
        await ConsumeExternalChallengeAsync(provider.Code, dto.ChallengeToken, ct);
        var profile = await verifier.ValidateAsync(provider.Code, dto.IdToken, dto.ChallengeToken, ct);
        var linked = await db.ExternalIdentities
            .Where(x => !x.IsDeleted && x.Provider == provider.Code && x.Subject == profile.Subject)
            .Include(x => x.User).ThenInclude(x => x.Roles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(ct);
        if (linked is not null)
        {
            EnsureActive(linked.User);
            linked.LastLoginAt = DateTime.UtcNow;
            linked.Email = profile.Email;
            db.LoginAudits.Add(new LoginAudit { UserId = linked.UserId, Identifier = $"{provider.Code}:{profile.Subject}", Succeeded = true, IpAddress = context?.IpAddress, UserAgent = context?.UserAgent });
            await db.SaveChangesAsync(ct);
            if (await RequiresTwoFactorAsync(linked.User, ct)) return await CreateTwoFactorChallengeAsync(linked.User, ct);
            return await IssueAsync(linked.User, ct, context);
        }

        if (!profile.EmailVerified || profile.Email is null || !provider.AllowEmailAutoLink)
            return new AuthResultDto(true, null, null, null, null, null, PrefillEmail: profile.Email, PrefillName: profile.DisplayName);

        var user = await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email == profile.Email, ct);
        if (user is null)
            return new AuthResultDto(true, null, null, null, null, null, PrefillEmail: profile.Email, PrefillName: profile.DisplayName);
        EnsureActive(user);
        await LinkIdentityAsync(user, profile, ct);
        db.LoginAudits.Add(new LoginAudit { UserId = user.Id, Identifier = $"{provider.Code}:{profile.Subject}", Succeeded = true, IpAddress = context?.IpAddress, UserAgent = context?.UserAgent });
        await db.SaveChangesAsync(ct);
        if (await RequiresTwoFactorAsync(user, ct)) return await CreateTwoFactorChallengeAsync(user, ct);
        return await IssueAsync(user, ct, context);
    }

    public async Task<LinkedExternalIdentityDto> LinkExternalAsync(Guid userId, ExternalLoginDto dto, CancellationToken ct = default)
    {
        var verifier = external ?? throw ApiException.BadRequest("تسجيل الدخول الخارجي غير مهيأ");
        var provider = verifier.Provider(dto.Provider);
        await ConsumeExternalChallengeAsync(provider.Code, dto.ChallengeToken, ct);
        var profile = await verifier.ValidateAsync(provider.Code, dto.IdToken, dto.ChallengeToken, ct);
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct) ?? throw ApiException.Unauthorized();
        EnsureActive(user);
        var existing = await db.ExternalIdentities.FirstOrDefaultAsync(x => !x.IsDeleted && x.Provider == provider.Code && x.Subject == profile.Subject, ct);
        if (existing is not null && existing.UserId != userId) throw ApiException.Conflict("هذا الحساب الخارجي مرتبط بمستخدم آخر");
        var link = existing ?? await LinkIdentityAsync(user, profile, ct);
        link.Email = profile.Email; link.LastLoginAt = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = user.TenantId, UserId = userId, Action = "auth.external_linked", EntityType = nameof(ExternalIdentity), EntityId = link.Id.ToString(), DataJson = System.Text.Json.JsonSerializer.Serialize(new { provider = provider.Code }) });
        await db.SaveChangesAsync(ct);
        return new(link.Provider, link.Email, link.LinkedAt, link.LastLoginAt);
    }

    public async Task<IReadOnlyList<LinkedExternalIdentityDto>> LinkedExternalAsync(Guid userId, CancellationToken ct = default) =>
        await db.ExternalIdentities.AsNoTracking().Where(x => !x.IsDeleted && x.UserId == userId)
            .OrderBy(x => x.Provider).Select(x => new LinkedExternalIdentityDto(x.Provider, x.Email, x.LinkedAt, x.LastLoginAt)).ToListAsync(ct);

    public async Task<AuthResultDto> RegisterCompanyAsync(RegisterCompanyDto dto, CancellationToken ct = default)
    {
        var phone = OtpService.NormalizePhone(dto.Phone);
        await otp.VerifyAsync(phone, dto.OtpCode, OtpPurpose.Registration, ct);

        if (string.IsNullOrWhiteSpace(dto.CompanyLegalName))
            throw ApiException.BadRequest("اسم الشركة مطلوب");
        if (string.IsNullOrWhiteSpace(dto.AdminFullName))
            throw ApiException.BadRequest("اسم المستخدم الإداري مطلوب");
        var minimumPasswordLength = await MinimumPasswordLengthAsync(ct);
        if (dto.AdminPassword.Length < minimumPasswordLength)
            throw ApiException.BadRequest($"كلمة المرور يجب ألا تقل عن {minimumPasswordLength} أحرف");
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
        var minimumPasswordLength = await MinimumPasswordLengthAsync(ct);
        if (dto.NewPassword.Length < minimumPasswordLength)
            throw ApiException.BadRequest($"كلمة المرور يجب ألا تقل عن {minimumPasswordLength} أحرف");

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

    private async Task EnsureNotLockedAsync(User? user, CancellationToken ct)
    {
        if (user is null || systemSettings is null) return;
        var attempts = await systemSettings.IntAsync("security", "lockAttempts", 5, ct);
        var minutes = await systemSettings.IntAsync("security", "lockoutMinutes", 15, ct);
        var since = DateTime.UtcNow.AddMinutes(-minutes);
        var lastSuccess = await db.LoginAudits.AsNoTracking().Where(x => x.UserId == user.Id && x.Succeeded).OrderByDescending(x => x.CreatedAt).Select(x => (DateTime?)x.CreatedAt).FirstOrDefaultAsync(ct);
        var lastSuccessAt = lastSuccess ?? DateTime.MinValue;
        var failures = await db.LoginAudits.AsNoTracking().CountAsync(x => x.UserId == user.Id && !x.Succeeded && x.CreatedAt >= since && x.CreatedAt > lastSuccessAt, ct);
        if (failures >= attempts) throw ApiException.Unauthorized($"تم قفل الحساب مؤقتًا. حاول بعد {minutes} دقيقة");
    }

    private Task<int> MinimumPasswordLengthAsync(CancellationToken ct) => systemSettings?.IntAsync("password-policy", "minLength", 8, ct) ?? Task.FromResult(8);

    private async Task<bool> RequiresTwoFactorAsync(User user, CancellationToken ct) => user.TwoFactorEnabled ||
        user.IsPlatformStaff && systemSettings is not null && await systemSettings.BoolAsync("security", "requireAdmin2fa", false, ct);

    private async Task ConsumeExternalChallengeAsync(string provider, string raw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 256) throw ApiException.Unauthorized("محاولة تسجيل الدخول الخارجي غير صالحة");
        var hash = TokenService.Hash(raw);
        var consumed = await db.ExternalAuthChallenges
            .Where(x => x.Provider == provider && x.TokenHash == hash && !x.Consumed && x.ExpiresAt > DateTime.UtcNow)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Consumed, true), ct);
        if (consumed != 1) throw ApiException.Unauthorized("انتهت محاولة تسجيل الدخول الخارجي، حاول مرة أخرى");
    }

    private async Task<ExternalIdentity> LinkIdentityAsync(User user, VerifiedExternalIdentity profile, CancellationToken ct)
    {
        var link = new ExternalIdentity
        {
            UserId = user.Id,
            Provider = profile.Provider,
            Subject = profile.Subject,
            Email = profile.Email,
            ProviderTenantId = profile.ProviderTenantId,
        };
        db.ExternalIdentities.Add(link);
        if (profile.EmailVerified && user.Email?.Equals(profile.Email, StringComparison.OrdinalIgnoreCase) == true) user.EmailVerified = true;
        await db.SaveChangesAsync(ct);
        return link;
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

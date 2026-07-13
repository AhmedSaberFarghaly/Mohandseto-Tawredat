namespace Mohandseto.Api.Application.Auth;

public record OtpRequestDto(string Phone, string Purpose);
public record OtpVerifyDto(string Phone, string Code);
public record EmailLoginDto(string Email, string Password);
public record RefreshDto(string RefreshToken);
public record TwoFactorLoginDto(string ChallengeToken, string Code);
public record PasswordResetRequestDto(string Email);
public record PasswordResetDto(string ResetToken, string Code, string NewPassword);
public record PasswordResetRequestResultDto(
    bool Sent,
    string ResetToken,
    DateTime ExpiresAt,
    string? MaskedPhone,
    string? DevelopmentCode);

public record RegisterCompanyDto(
    string Phone,
    string OtpCode,
    string CompanyLegalName,
    string? CompanyLegalNameEn,
    string? CommercialRegistrationNo,
    string? TaxCardNo,
    string Governorate,
    string? City,
    string? AddressLine,
    string? Industry,
    string AdminFullName,
    string? AdminEmail,
    string AdminPassword);

public record AuthUserDto(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    Guid? TenantId,
    bool IsPlatformStaff,
    string? TenantStatus,
    IReadOnlyList<string> Roles);

public record AuthResultDto(
    bool IsNewUser,
    AuthUserDto? User,
    string? AccessToken,
    DateTime? AccessExpiresAt,
    string? RefreshToken,
    DateTime? RefreshExpiresAt,
    bool RequiresTwoFactor = false,
    string? ChallengeToken = null,
    DateTime? ChallengeExpiresAt = null,
    string? DevelopmentCode = null);

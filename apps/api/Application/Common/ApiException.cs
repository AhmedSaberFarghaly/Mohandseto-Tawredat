namespace Mohandseto.Api.Application.Common;

/// <summary>
/// Domain/service error surfaced to clients as ProblemDetails with an Arabic message.
/// Never carries stack traces to the client.
/// </summary>
public class ApiException(int statusCode, string messageAr, string? code = null) : Exception(messageAr)
{
    public int StatusCode { get; } = statusCode;
    public string? Code { get; } = code;

    public static ApiException BadRequest(string messageAr, string? code = null) => new(400, messageAr, code);
    public static ApiException Unauthorized(string messageAr = "غير مصرح بالدخول") => new(401, messageAr);
    public static ApiException Forbidden(string messageAr = "لا تملك صلاحية تنفيذ هذا الإجراء") => new(403, messageAr);
    public static ApiException NotFound(string messageAr = "العنصر غير موجود") => new(404, messageAr);
    public static ApiException Conflict(string messageAr, string? code = null) => new(409, messageAr, code);
    public static ApiException TooMany(string messageAr = "عدد محاولات كبير، حاول لاحقًا") => new(429, messageAr);
}

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace uShopDemo.Api.Sessions;

/// <summary>Issues and validates the signed, time-limited session cookie. The cookie value is the protected session id.</summary>
public sealed class SessionCookie(IDataProtectionProvider provider)
{
    public const string Purpose = "ushop.session.v1";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private readonly ITimeLimitedDataProtector protector =
        provider.CreateProtector(Purpose).ToTimeLimitedDataProtector();

    public static string CookieName(HttpRequest request) =>
        $"__Host-ushop-session-{request.Host.Port ?? (request.IsHttps ? 443 : 80)}";

    public static Guid NewSessionId() => new(RandomNumberGenerator.GetBytes(16));

    /// <summary>The session id in the request cookie, or null if missing, tampered with or expired.</summary>
    public Guid? Read(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(CookieName(request), out var value) || string.IsNullOrEmpty(value))
            return null;
        try
        {
            return Guid.TryParseExact(protector.Unprotect(value), "N", out var id) ? id : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public void Issue(HttpContext context, Guid sessionId) =>
        context.Response.Cookies.Append(
            CookieName(context.Request),
            protector.Protect(sessionId.ToString("N"), Lifetime),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                MaxAge = Lifetime,
                IsEssential = true,
            });
}

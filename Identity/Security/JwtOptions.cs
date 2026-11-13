using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Identity.Security;

public class JwtOptions : IValidatableObject
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC-SHA256 signing key. Supply via user-secrets / environment, never appsettings.</summary>
    [Required] public string SigningKey { get; set; } = string.Empty;

    [Range(1, 60)] public int AccessTokenMinutes { get; set; } = 10;
    [Range(1, 90)] public int RefreshTokenDays { get; set; } = 7;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            yield return new ValidationResult(
                "Jwt:SigningKey must be at least 32 bytes for HMAC-SHA256.", new[] { nameof(SigningKey) });
        }
    }
}

public static class RefreshCookie
{
    public const string Name = "refreshToken";

    /// <summary>
    /// Scoped to the auth controller so refresh, logout and change-password all receive it,
    /// while no other API route ever sees the cookie.
    /// </summary>
    public const string Path = "/api/auth";

    /// <summary>
    /// None so a credentialed cross-origin SPA (see <see cref="CorsOptions"/>) can send it; always paired with Secure.
    /// CSRF exposure is limited because CORS only lets the allowed origins read the refresh/login responses.
    /// </summary>
    public const Microsoft.AspNetCore.Http.SameSiteMode SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
}

public class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "AuthClients";

    /// <summary>Exact origins (scheme://host:port). Wildcards are not allowed with credentials.</summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string StaffOrAdmin = "StaffOrAdmin";
    public const string Customer = "Customer";
}

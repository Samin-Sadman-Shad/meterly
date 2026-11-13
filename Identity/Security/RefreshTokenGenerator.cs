using System.Security.Cryptography;
using System.Text;

namespace Identity.Security;

public static class RefreshTokenGenerator
{
    /// <summary>64 random bytes, URL-safe base64 so it is cookie-safe without encoding.</summary>
    public static string Generate()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>SHA-256 hex digest; only this value is stored in the database.</summary>
    public static string Hash(string plainToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken)));
}

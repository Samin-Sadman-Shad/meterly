using Identity.Model;

namespace Identity.Contracts.Repositories;

public interface IRefreshTokenRepository
{
    /// <summary>Finds a token by its plaintext value (hashed internally before lookup).</summary>
    Task<RefreshToken?> GetByTokenAsync(string plainToken, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetLatestByUserAndFamilyAsync(Guid userId, Guid familyId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the token state change. Returns false when another request changed the token first
    /// (optimistic concurrency conflict), which callers treat as reuse.
    /// </summary>
    Task<bool> UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes every non-revoked token in a family. When <paramref name="userId"/> is supplied, only that
    /// user's tokens are touched. Returns the number of tokens revoked.
    /// </summary>
    Task<int> RevokeTokenByFamilyAsync(Guid familyId, Guid? userId = null, CancellationToken cancellationToken = default);

    Task<int> RevokeTokenByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// In one save: marks all unused, non-revoked tokens of (userId, familyId) as used and adds <paramref name="newToken"/>.
    /// </summary>
    Task InvalidatePreviousAndSaveAsync(RefreshToken newToken, CancellationToken cancellationToken = default);
}

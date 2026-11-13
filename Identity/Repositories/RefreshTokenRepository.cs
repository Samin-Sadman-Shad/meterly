using Identity.Context;
using Identity.Contracts.Repositories;
using Identity.Model;
using Identity.Security;
using Microsoft.EntityFrameworkCore;

namespace Identity.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _db;

    public RefreshTokenRepository(AuthDbContext db) => _db = db;

    public Task<RefreshToken?> GetByTokenAsync(string plainToken, CancellationToken cancellationToken = default)
    {
        var hash = RefreshTokenGenerator.Hash(plainToken);
        return _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
    }

    public Task<RefreshToken?> GetLatestByUserAndFamilyAsync(Guid userId, Guid familyId, CancellationToken cancellationToken = default)
        => _db.RefreshTokens
            .Where(t => t.UserId == userId && t.FamilyId == familyId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> HasActiveTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return _db.RefreshTokens.AnyAsync(
            t => t.UserId == userId && !t.IsRevoked && !t.IsUsed && t.ExpiresAt > now, cancellationToken);
    }

    public async Task<bool> UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Reload so the tracked entity reflects what the winning request wrote.
            await _db.Entry(token).ReloadAsync(cancellationToken);
            return false;
        }
    }

    public async Task<int> RevokeTokenByFamilyAsync(Guid familyId, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.RefreshTokens.Where(t => t.FamilyId == familyId && !t.IsRevoked);
        if (userId is { } owner)
        {
            query = query.Where(t => t.UserId == owner);
        }
        return await RevokeAsync(query, cancellationToken);
    }

    public Task<int> RevokeTokenByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => RevokeAsync(_db.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked), cancellationToken);

    public async Task InvalidatePreviousAndSaveAsync(RefreshToken newToken, CancellationToken cancellationToken = default)
    {
        var previous = await _db.RefreshTokens
            .Where(t => t.UserId == newToken.UserId && t.FamilyId == newToken.FamilyId && !t.IsUsed && !t.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in previous)
        {
            token.MarkUsed();
        }

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> RevokeAsync(IQueryable<RefreshToken> query, CancellationToken cancellationToken)
    {
        // Revocation must win against a concurrent refresh, so retry on concurrency conflicts.
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            var tokens = await query.ToListAsync(cancellationToken);
            foreach (var token in tokens)
            {
                token.Revoke();
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return tokens.Count;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < maxAttempts)
            {
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(cancellationToken);
                }
            }
        }
    }
}

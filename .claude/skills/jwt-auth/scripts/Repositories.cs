
using System.Security.Cryptography;

namespace JwtAuth.Reference;

public class EfUserRepository : IUserRepository<User>
{
    private readonly AuthDbContext _db;
    public EfUserRepository(AuthDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByUserNameOrEmailAsync(string identifier)
        => _db.Users.FirstOrDefaultAsync(u => u.UserName == identifier || u.Email == identifier);

    public Task<bool> ExistsAsync(string userName, string email)
        => _db.Users.AnyAsync(u => u.UserName == userName || u.Email == email);

    public async Task AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }
}

public class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _db;
    public EfRefreshTokenRepository(AuthDbContext db) => _db = db;

    public Task<RefreshToken?> GetByTokenAsync(string token)
        => _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);

    public Task<RefreshToken?> GetLatestByUserAndFamilyAsync(Guid userId, Guid familyId)
        => _db.RefreshTokens
            .Where(t => t.UserId == userId && t.FamilyId == familyId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task SaveAsync(RefreshToken token)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(RefreshToken token)
    {
        _db.RefreshTokens.Update(token);
        await _db.SaveChangesAsync();
    }

    public async Task RevokeTokenByFamilyAsync(Guid familyId)
    {
        var tokens = await _db.RefreshTokens.Where(t => t.FamilyId == familyId && !t.IsRevoked).ToListAsync();
        foreach (var token in tokens)
        {
            await token.RevokeAsync();
        }
        await _db.SaveChangesAsync();
    }

    public async Task RevokeTokenByUserIdAsync(Guid userId)
    {
        var tokens = await _db.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync();
        foreach (var token in tokens)
        {
            await token.RevokeAsync();
        }
        await _db.SaveChangesAsync();
    }

    public async Task InvalidatePreviousTokensAsync(Guid userId, Guid familyId)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.FamilyId == familyId && !t.IsUsed && !t.IsRevoked)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsUsed = true;
        }
        await _db.SaveChangesAsync();
    }

    public Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, Guid familyId, TimeSpan lifetime)
    {
        var token = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            UserId = userId,
            FamilyId = familyId,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(lifetime)
        };
        return Task.FromResult(token);
    }
}
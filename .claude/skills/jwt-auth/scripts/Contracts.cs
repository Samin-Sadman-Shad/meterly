

namespace JwtAuth.Reference;

public interface IUserRepository<TUser> where TUser : class
{
    Task<TUser?> GetByIdAsync(Guid id);
    Task<TUser?> GetByUserNameOrEmailAsync(string identifier);
    Task<bool> ExistsAsync(string userName, string email);
    Task AddAsync(TUser user);
    Task UpdateAsync(TUser user);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken?> GetLatestByUserAndFamilyAsync(Guid userId, Guid familyId);
    Task SaveAsync(RefreshToken token);
    Task UpdateAsync(RefreshToken token);
    Task RevokeTokenByFamilyAsync(Guid familyId);
    Task RevokeTokenByUserIdAsync(Guid userId);
    Task InvalidatePreviousTokensAsync(Guid userId, Guid familyId);
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, Guid familyId, TimeSpan lifetime);
}

public interface ISecurityNotificationPublisher
{
    Task PublishTokenReuseDetectedAsync(Guid userId, Guid familyId, DateTime detectedAtUtc);
}
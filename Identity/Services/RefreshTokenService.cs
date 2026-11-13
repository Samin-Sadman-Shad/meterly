using Identity.Contracts.Repositories;
using Identity.Contracts.Services;
using Identity.DTOs;
using Identity.Model;
using Identity.Security;
using Microsoft.Extensions.Options;

namespace Identity.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _tokenRepo;
    private readonly TimeSpan _lifetime;

    public RefreshTokenService(IRefreshTokenRepository tokenRepo, IOptions<JwtOptions> options)
    {
        _tokenRepo = tokenRepo;
        _lifetime = TimeSpan.FromDays(options.Value.RefreshTokenDays);
    }

    public Task<RefreshToken?> GetByTokenAsync(string plainToken, CancellationToken cancellationToken = default)
        => _tokenRepo.GetByTokenAsync(plainToken, cancellationToken);

    public async Task<IssuedRefreshToken> RotateAsync(Guid userId, Guid familyId, CancellationToken cancellationToken = default)
    {
        var plainToken = RefreshTokenGenerator.Generate();
        var now = DateTime.UtcNow;
        var entity = new RefreshToken
        {
            TokenHash = RefreshTokenGenerator.Hash(plainToken),
            UserId = userId,
            FamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = now.Add(_lifetime)
        };

        await _tokenRepo.InvalidatePreviousAndSaveAsync(entity, cancellationToken);
        return new IssuedRefreshToken(familyId, plainToken, entity.ExpiresAt);
    }

    public Task<bool> MarkAsUsedAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        token.MarkUsed();
        return _tokenRepo.UpdateAsync(token, cancellationToken);
    }

    public Task<bool> HasActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        => _tokenRepo.HasActiveTokensAsync(userId, cancellationToken);

    public Task<int> RevokeFamilyAsync(Guid familyId, Guid? userId = null, CancellationToken cancellationToken = default)
        => _tokenRepo.RevokeTokenByFamilyAsync(familyId, userId, cancellationToken);

    public Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => _tokenRepo.RevokeTokenByUserIdAsync(userId, cancellationToken);
}

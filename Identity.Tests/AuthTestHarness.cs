using Identity.Context;
using Identity.Contracts.Services;
using Identity.Model;
using Identity.Repositories;
using Identity.Security;
using Identity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Identity.Tests;

internal sealed class FakeSecurityNotificationPublisher : ISecurityNotificationPublisher
{
    public List<(Guid UserId, Guid FamilyId, DateTime DetectedAtUtc, bool HasOtherActiveSessions)> PublishedAlerts { get; } = new();

    public Task PublishTokenReuseDetectedAsync(
        Guid userId, Guid familyId, DateTime detectedAtUtc, bool hasOtherActiveSessions, CancellationToken cancellationToken = default)
    {
        PublishedAlerts.Add((userId, familyId, detectedAtUtc, hasOtherActiveSessions));
        return Task.CompletedTask;
    }
}

/// <summary>Wires the real services over an EF Core in-memory store. Several harnesses may share one store.</summary>
internal sealed class AuthTestHarness : IDisposable
{
    public static readonly IOptions<JwtOptions> JwtOptions = Options.Create(new JwtOptions
    {
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        SigningKey = "SuperSecretSigningKeyForUnitTestingAtLeast32Bytes!",
        AccessTokenMinutes = 10,
        RefreshTokenDays = 7
    });

    public AuthDbContext Db { get; }
    public AuthService Auth { get; }
    public RefreshTokenService Tokens { get; }
    public FakeSecurityNotificationPublisher Broker { get; } = new();

    public AuthTestHarness(string? storeName = null)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(storeName ?? Guid.NewGuid().ToString())
            .Options;

        Db = new AuthDbContext(options);
        Db.Database.EnsureCreated();

        var userService = new UserService(new UserRepository(Db), new IdentityPasswordHasherAdapter<User>());
        Tokens = new RefreshTokenService(new RefreshTokenRepository(Db), JwtOptions);
        Auth = new AuthService(userService, Tokens, new JwtAccessTokenService(JwtOptions), Broker);
    }

    public Task<RefreshToken> TokenEntityAsync(string plainToken)
    {
        var hash = RefreshTokenGenerator.Hash(plainToken);
        return Db.RefreshTokens.AsNoTracking().SingleAsync(t => t.TokenHash == hash);
    }

    public void Dispose() => Db.Dispose();
}

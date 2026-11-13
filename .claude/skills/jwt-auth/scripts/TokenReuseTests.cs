

// Reuse Detection Test Script (`.claude/skills/jwt-auth/scripts/TokenReuseTests.cs`)

using JwtAuth.Reference;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JwtAuth.Tests;

public class TokenReuseTests
{
    private sealed class FakeSecurityNotificationPublisher : ISecurityNotificationPublisher
    {
        public List<(Guid UserId, Guid FamilyId, DateTime DetectedAtUtc)> PublishedAlerts { get; } = new();

        public Task PublishTokenReuseDetectedAsync(Guid userId, Guid familyId, DateTime detectedAtUtc)
        {
            PublishedAlerts.Add((userId, familyId, detectedAtUtc));
            return Task.CompletedTask;
        }
    }

    private static (AuthService Service, AuthDbContext Db, FakeSecurityNotificationPublisher Broker) CreateSut()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AuthDbContext(options);
        var userRepo = new EfUserRepository(db);
        var tokenRepo = new EfRefreshTokenRepository(db);
        var passwordHasher = new IdentityPasswordHasherAdapter<User>();

        var userService = new UserService(userRepo, passwordHasher);
        var tokenService = new RefreshTokenService(tokenRepo);
        var accessTokenService = new JwtAccessTokenService(
            secretKey: "SuperSecretSigningKeyForUnitTestingAtLeast32Bytes!",
            issuer: "TestIssuer",
            audience: "TestAudience");
        var broker = new FakeSecurityNotificationPublisher();

        var authService = new AuthService(userService, tokenService, accessTokenService, broker);
        return (authService, db, broker);
    }

    [Fact]
    public async Task StolenTokenReplay_RevokesCompromisedFamily_NotifiesBroker_AndKeepsOtherDevicesActive()
    {
        // Arrange
        var (authService, db, broker) = CreateSut();

        // 1. UserA registers on LaptopA and receives TokenA (familyId = laptopA)
        var laptopLoginResult = await authService.RegisterAsync(
            new RegisterRequest("userA", "usera@example.com", "StrongPass1!"));
        Assert.True(laptopLoginResult.Succeeded);

        var tokenA = laptopLoginResult.RefreshToken!;
        var tokenAEntity = await db.RefreshTokens.SingleAsync(t => t.Token == tokenA);
        var laptopAFamilyId = tokenAEntity.FamilyId;
        var userId = tokenAEntity.UserId;
        Assert.False(tokenAEntity.IsUsed);

        // UserA also logs in on PhoneB (separate familyId = phoneB)
        var phoneLoginResult = await authService.LoginAsync(
            new LoginRequest("userA", "StrongPass1!"));
        Assert.True(phoneLoginResult.Succeeded);
        var phoneToken = phoneLoginResult.RefreshToken!;

        // 2. Hacker steals TokenA while IsUsed == false and calls /api/auth/refresh
        var hackerRefreshResult = await authService.RefreshAsync(tokenA);
        Assert.True(hackerRefreshResult.Succeeded);

        var tokenB = hackerRefreshResult.RefreshToken!;
        var refreshedTokenA = await db.RefreshTokens.SingleAsync(t => t.Token == tokenA);
        var tokenBEntity = await db.RefreshTokens.SingleAsync(t => t.Token == tokenB);

        // Server issued TokenB under familyId = laptopA and marked TokenA as IsUsed = true
        Assert.True(refreshedTokenA.IsUsed);
        Assert.Equal(laptopAFamilyId, tokenBEntity.FamilyId);
        Assert.False(tokenBEntity.IsUsed);

        // 3. After 10 minutes, UserA's LaptopA sends TokenA to /api/auth/refresh
        var legitimateLaptopRefresh = await authService.RefreshAsync(tokenA);

        // Server detects TokenA.IsUsed == true -> revokes laptopA family and returns Unauthorized
        Assert.False(legitimateLaptopRefresh.Succeeded);

        var finalTokenA = await db.RefreshTokens.SingleAsync(t => t.Token == tokenA);
        var finalTokenB = await db.RefreshTokens.SingleAsync(t => t.Token == tokenB);

        // Both TokenA and Hacker's TokenB are revoked
        Assert.True(finalTokenA.IsRevoked);
        Assert.True(finalTokenB.IsRevoked);

        // Message broker received breach notification for UserA + laptopA family
        var alert = Assert.Single(broker.PublishedAlerts);
        Assert.Equal(userId, alert.UserId);
        Assert.Equal(laptopAFamilyId, alert.FamilyId);

        // Hacker's TokenB is now rejected on next attempt (both sessions on LaptopA terminated)
        var hackerFollowUpAttempt = await authService.RefreshAsync(tokenB);
        Assert.False(hackerFollowUpAttempt.Succeeded);

        // 4. UserA's session on PhoneB (other familyId) remains active and unaffected
        var phoneRefreshResult = await authService.RefreshAsync(phoneToken);
        Assert.True(phoneRefreshResult.Succeeded);
    }
}
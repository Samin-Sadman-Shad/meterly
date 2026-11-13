using Identity.DTOs;

namespace Identity.Tests;

public class TokenReuseTests
{
    private static readonly RegisterRequest UserA = new("userA", "usera@example.com", "StrongPass1!", "User", "A");

    [Fact]
    public async Task StolenTokenReplay_RevokesCompromisedFamily_NotifiesBroker_AndKeepsOtherDevicesActive()
    {
        using var sut = new AuthTestHarness();

        // 1. UserA registers on LaptopA and receives TokenA (familyId = laptopA)
        var laptop = await sut.Auth.RegisterAsync(UserA);
        Assert.True(laptop.Succeeded);
        var tokenA = laptop.RefreshToken!.PlainToken;
        var tokenAEntity = await sut.TokenEntityAsync(tokenA);
        var laptopFamily = tokenAEntity.FamilyId;
        var userId = tokenAEntity.UserId;
        Assert.False(tokenAEntity.IsUsed);

        // 2. UserA also has a session on PhoneB (separate family)
        var phone = await sut.Auth.LoginAsync(new LoginRequest("userA", "StrongPass1!"));
        Assert.True(phone.Succeeded);
        Assert.NotEqual(laptopFamily, phone.RefreshToken!.FamilyId);

        // 3-4. Hacker replays TokenA first and gets TokenB in the laptop family
        var hacker = await sut.Auth.RefreshTokenAsync(tokenA);
        Assert.True(hacker.Succeeded);
        var tokenB = hacker.RefreshToken!.PlainToken;
        Assert.True((await sut.TokenEntityAsync(tokenA)).IsUsed);
        var tokenBEntity = await sut.TokenEntityAsync(tokenB);
        Assert.Equal(laptopFamily, tokenBEntity.FamilyId);
        Assert.False(tokenBEntity.IsUsed);

        // 5-6. The real laptop later presents TokenA -> reuse detected
        var laptopRetry = await sut.Auth.RefreshTokenAsync(tokenA);
        Assert.False(laptopRetry.Succeeded);
        Assert.Equal(AuthErrorKind.Unauthorized, laptopRetry.Error!.Kind);
        Assert.Null(laptopRetry.RefreshToken);
        Assert.NotNull(laptopRetry.Warning);

        Assert.True((await sut.TokenEntityAsync(tokenA)).IsRevoked);
        Assert.True((await sut.TokenEntityAsync(tokenB)).IsRevoked);

        var alert = Assert.Single(sut.Broker.PublishedAlerts);
        Assert.Equal(userId, alert.UserId);
        Assert.Equal(laptopFamily, alert.FamilyId);
        Assert.True(alert.HasOtherActiveSessions); // PhoneB is still alive

        // 7. Hacker's TokenB is dead; PhoneB is unaffected
        Assert.False((await sut.Auth.RefreshTokenAsync(tokenB)).Succeeded);
        Assert.True((await sut.Auth.RefreshTokenAsync(phone.RefreshToken.PlainToken)).Succeeded);
    }

    [Fact]
    public async Task Reuse_OnLastSession_ReportsNoOtherActiveSessions()
    {
        using var sut = new AuthTestHarness();
        var registered = await sut.Auth.RegisterAsync(UserA);
        var token = registered.RefreshToken!.PlainToken;

        Assert.True((await sut.Auth.RefreshTokenAsync(token)).Succeeded);
        var replay = await sut.Auth.RefreshTokenAsync(token);

        Assert.False(replay.Succeeded);
        Assert.Contains("no other active sessions", replay.Warning);
        Assert.False(Assert.Single(sut.Broker.PublishedAlerts).HasOtherActiveSessions);
    }

    [Fact]
    public async Task ConcurrentRefresh_WithSameToken_OnlyOneRequestCanConsumeIt()
    {
        var store = Guid.NewGuid().ToString();
        using var seed = new AuthTestHarness(store);
        var token = (await seed.Auth.RegisterAsync(UserA)).RefreshToken!.PlainToken;

        // Two requests (separate DbContexts) load the same unused token before either saves.
        using var requestOne = new AuthTestHarness(store);
        using var requestTwo = new AuthTestHarness(store);
        var loadedByOne = await requestOne.Tokens.GetByTokenAsync(token);
        var loadedByTwo = await requestTwo.Tokens.GetByTokenAsync(token);

        Assert.True(await requestOne.Tokens.MarkAsUsedAsync(loadedByOne!));
        Assert.False(await requestTwo.Tokens.MarkAsUsedAsync(loadedByTwo!));
    }

    [Fact]
    public async Task Refresh_RejectsMissingUnknownAndRevokedTokens()
    {
        using var sut = new AuthTestHarness();
        var token = (await sut.Auth.RegisterAsync(UserA)).RefreshToken!.PlainToken;

        Assert.False((await sut.Auth.RefreshTokenAsync(null)).Succeeded);
        Assert.False((await sut.Auth.RefreshTokenAsync("not-a-real-token")).Succeeded);

        await sut.Auth.LogoutAsync(token);
        var afterLogout = await sut.Auth.RefreshTokenAsync(token);
        Assert.False(afterLogout.Succeeded);
        Assert.Empty(sut.Broker.PublishedAlerts); // revoked != reused
    }
}

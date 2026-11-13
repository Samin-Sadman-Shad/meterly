using Microsoft.EntityFrameworkCore;
using Identity.DTOs;
using Identity.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Identity.Tests;

public class AuthFlowTests
{
    private static readonly RegisterRequest UserA = new("userA", "usera@example.com", "StrongPass1!", "User", "A");

    [Fact]
    public async Task Register_IssuesTokensWithIdentityAndDefaultRoleClaims()
    {
        using var sut = new AuthTestHarness();
        var result = await sut.Auth.RegisterAsync(UserA);

        Assert.True(result.Succeeded);
        Assert.Equal([AppRoles.Customer], result.UserData!.Roles);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal(result.UserData.Id.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("usera@example.com", jwt.Claims.Single(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal(AppRoles.Customer, jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("TestIssuer", jwt.Issuer);
    }

    [Fact]
    public async Task Roles_PersistedOnUser_RoundTripIntoAccessTokenRoleClaims()
    {
        var store = Guid.NewGuid().ToString();
        using (var setup = new AuthTestHarness(store))
        {
            var registered = await setup.Auth.RegisterAsync(UserA);
            var user = await setup.Db.Users.SingleAsync(u => u.Id == registered.UserData!.Id);
            user.Roles.Add(AppRoles.Admin);
            await setup.Db.SaveChangesAsync();
        }

        using var sut = new AuthTestHarness(store); // fresh context: roles come back from the store
        var login = await sut.Auth.LoginAsync(new LoginRequest("userA", "StrongPass1!"));

        var roleClaims = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken)
            .Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
        Assert.Equal([AppRoles.Customer, AppRoles.Admin], roleClaims);
    }

    [Fact]
    public async Task Register_RejectsDuplicateUserNameOrEmail_CaseInsensitively()
    {
        using var sut = new AuthTestHarness();
        await sut.Auth.RegisterAsync(UserA);

        var duplicate = await sut.Auth.RegisterAsync(UserA with { UserName = "other", Email = "USERA@example.com" });

        Assert.Equal(AuthErrorKind.Conflict, duplicate.Error!.Kind);
    }

    [Fact]
    public async Task Register_RejectsWeakPassword_WithRuleErrors()
    {
        using var sut = new AuthTestHarness();
        var result = await sut.Auth.RegisterAsync(UserA with { Password = "weak" });

        Assert.Equal(AuthErrorKind.Validation, result.Error!.Kind);
        Assert.Contains(result.Error.Details!, e => e.Contains("8 characters"));
    }

    [Theory]
    [InlineData("userA", "WrongPass1!")]
    [InlineData("nobody", "StrongPass1!")]
    public async Task Login_FailsWithSameGenericError(string identifier, string password)
    {
        using var sut = new AuthTestHarness();
        await sut.Auth.RegisterAsync(UserA);

        var result = await sut.Auth.LoginAsync(new LoginRequest(identifier, password));

        Assert.Equal(AuthErrorKind.Unauthorized, result.Error!.Kind);
        Assert.Equal("Invalid credentials.", result.Error.Message);
    }

    [Fact]
    public async Task Login_AcceptsEmailAsIdentifier()
    {
        using var sut = new AuthTestHarness();
        await sut.Auth.RegisterAsync(UserA);

        Assert.True((await sut.Auth.LoginAsync(new LoginRequest("UserA@Example.com", "StrongPass1!"))).Succeeded);
    }

    [Fact]
    public async Task ChangePassword_Succeeds_RotatesSameFamily_AndOldPasswordStopsWorking()
    {
        using var sut = new AuthTestHarness();
        var registered = await sut.Auth.RegisterAsync(UserA);
        var userId = registered.UserData!.Id;
        var oldToken = registered.RefreshToken!;

        var changed = await sut.Auth.ChangePasswordAsync(userId, oldToken.PlainToken,
            new ChangePasswordRequest("StrongPass1!", "EvenStronger2@"));

        Assert.True(changed.Succeeded);
        Assert.Equal(oldToken.FamilyId, changed.RefreshToken!.FamilyId);
        Assert.True((await sut.TokenEntityAsync(oldToken.PlainToken)).IsUsed);
        Assert.False((await sut.Auth.LoginAsync(new LoginRequest("userA", "StrongPass1!"))).Succeeded);
        Assert.True((await sut.Auth.LoginAsync(new LoginRequest("userA", "EvenStronger2@"))).Succeeded);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_IsUnauthorized()
    {
        using var sut = new AuthTestHarness();
        var registered = await sut.Auth.RegisterAsync(UserA);

        var result = await sut.Auth.ChangePasswordAsync(registered.UserData!.Id, registered.RefreshToken!.PlainToken,
            new ChangePasswordRequest("WrongPass1!", "EvenStronger2@"));

        Assert.Equal(AuthErrorKind.Unauthorized, result.Error!.Kind);
    }

    [Fact]
    public async Task ChangePassword_WithUsedRefreshToken_IsUnauthorized()
    {
        using var sut = new AuthTestHarness();
        var registered = await sut.Auth.RegisterAsync(UserA);
        await sut.Auth.RefreshTokenAsync(registered.RefreshToken!.PlainToken); // consumes it

        var result = await sut.Auth.ChangePasswordAsync(registered.UserData!.Id, registered.RefreshToken.PlainToken,
            new ChangePasswordRequest("StrongPass1!", "EvenStronger2@"));

        Assert.Equal(AuthErrorKind.Unauthorized, result.Error!.Kind);
    }

    [Theory]
    [InlineData("StrongPass1!")]   // same as current
    [InlineData("nouppercase1!")]
    public async Task ChangePassword_InvalidNewPassword_IsValidationError(string newPassword)
    {
        using var sut = new AuthTestHarness();
        var registered = await sut.Auth.RegisterAsync(UserA);

        var result = await sut.Auth.ChangePasswordAsync(registered.UserData!.Id, registered.RefreshToken!.PlainToken,
            new ChangePasswordRequest("StrongPass1!", newPassword));

        Assert.Equal(AuthErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public async Task RevokeAllSessions_KillsEveryDevice()
    {
        using var sut = new AuthTestHarness();
        var laptop = await sut.Auth.RegisterAsync(UserA);
        var phone = await sut.Auth.LoginAsync(new LoginRequest("userA", "StrongPass1!"));

        await sut.Auth.RevokeAllSessionsAsync(laptop.UserData!.Id);

        Assert.False((await sut.Auth.RefreshTokenAsync(laptop.RefreshToken!.PlainToken)).Succeeded);
        Assert.False((await sut.Auth.RefreshTokenAsync(phone.RefreshToken!.PlainToken)).Succeeded);
    }

    [Fact]
    public async Task RevokeSessionByFamily_CannotTouchAnotherUsersSession()
    {
        using var sut = new AuthTestHarness();
        var victim = await sut.Auth.RegisterAsync(UserA);
        var attacker = await sut.Auth.RegisterAsync(new RegisterRequest("userB", "userb@example.com", "StrongPass1!", "User", "B"));

        var revoked = await sut.Auth.RevokeSessionsByFamilyIdAsync(attacker.UserData!.Id, victim.RefreshToken!.FamilyId);

        Assert.False(revoked);
        Assert.True((await sut.Auth.RefreshTokenAsync(victim.RefreshToken.PlainToken)).Succeeded);
    }
}

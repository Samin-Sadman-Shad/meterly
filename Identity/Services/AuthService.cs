using Identity.Contracts.Services;
using Identity.DTOs;
using Identity.Model;

namespace Identity.Services;

/// <summary>Orchestrates every auth endpoint workflow; controllers only map HTTP in and out.</summary>
public class AuthService : IAuthService
{
    private const string InvalidSession = "Session expired or invalid refresh token.";

    private readonly IUserService _userService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly ISecurityNotificationPublisher _notificationPublisher;

    public AuthService(
        IUserService userService,
        IRefreshTokenService refreshTokenService,
        IAccessTokenService accessTokenService,
        ISecurityNotificationPublisher notificationPublisher)
    {
        _userService = userService;
        _refreshTokenService = refreshTokenService;
        _accessTokenService = accessTokenService;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<AuthResult> IssueTokenAsync(User user, Guid familyId, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _refreshTokenService.RotateAsync(user.Id, familyId, cancellationToken);
        var accessToken = _accessTokenService.GenerateAccessToken(user);
        var userData = new UserDataDto(user.Id, user.UserName, user.Email, user.FirstName, user.LastName, user.Roles);

        return new AuthResult(true, RefreshToken: refreshToken, AccessToken: accessToken, UserData: userData);
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var (user, error) = await _userService.RegisterUserAsync(request, cancellationToken);
        if (user is null)
        {
            return new AuthResult(false, error);
        }

        return await IssueTokenAsync(user, Guid.NewGuid(), cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userService.ValidateCredentialsAsync(request.Identifier, request.Password, cancellationToken);
        if (user is null)
        {
            return AuthResult.Fail(AuthErrorKind.Unauthorized, "Invalid credentials.");
        }

        // A fresh family per login = one independently revocable session per device.
        return await IssueTokenAsync(user, Guid.NewGuid(), cancellationToken);
    }

    public async Task<AuthResult> RefreshTokenAsync(string? incomingRefreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(incomingRefreshToken))
        {
            return AuthResult.Fail(AuthErrorKind.Unauthorized, "Refresh token is missing.");
        }

        var storedToken = await _refreshTokenService.GetByTokenAsync(incomingRefreshToken, cancellationToken);
        if (storedToken is null || storedToken.IsRevoked || storedToken.IsExpired())
        {
            return AuthResult.Fail(AuthErrorKind.Unauthorized, "Refresh token is invalid or expired.");
        }

        if (storedToken.IsUsed)
        {
            return await HandleReuseAsync(storedToken, cancellationToken);
        }

        var user = await _userService.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null)
        {
            return AuthResult.Fail(AuthErrorKind.Unauthorized, "User not found.");
        }

        // Losing the optimistic-concurrency race means another request presented this token at the same moment.
        if (!await _refreshTokenService.MarkAsUsedAsync(storedToken, cancellationToken))
        {
            return await HandleReuseAsync(storedToken, cancellationToken);
        }

        return await IssueTokenAsync(user, storedToken.FamilyId, cancellationToken);
    }

    public async Task LogoutAsync(string? incomingRefreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(incomingRefreshToken)) return;

        var storedToken = await _refreshTokenService.GetByTokenAsync(incomingRefreshToken, cancellationToken);
        if (storedToken is not null)
        {
            await _refreshTokenService.RevokeFamilyAsync(storedToken.FamilyId, cancellationToken: cancellationToken);
        }
    }

    public Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        => _refreshTokenService.RevokeAllForUserAsync(userId, cancellationToken);

    public async Task<bool> RevokeSessionsByFamilyIdAsync(Guid userId, Guid familyId, CancellationToken cancellationToken = default)
        // Scoped to userId so a user can never revoke another user's session.
        => await _refreshTokenService.RevokeFamilyAsync(familyId, userId, cancellationToken) > 0;

    public async Task<AuthResult> ChangePasswordAsync(
        Guid userId, string? incomingRefreshToken, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetByIdAsync(userId, cancellationToken);
        if (user is null || !await _userService.VerifyPasswordAsync(user, request.CurrentPassword, cancellationToken))
        {
            return AuthResult.Fail(AuthErrorKind.Unauthorized, "Current password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(incomingRefreshToken))
        {
            return AuthResult.Fail(AuthErrorKind.Unauthorized, InvalidSession);
        }

        var currentToken = await _refreshTokenService.GetByTokenAsync(incomingRefreshToken, cancellationToken);
        if (currentToken is null || currentToken.UserId != userId || !currentToken.IsActive())
        {
            return AuthResult.Fail(AuthErrorKind.Unauthorized, InvalidSession);
        }

        var errors = _userService.ValidateNewPassword(request.NewPassword, request.CurrentPassword);
        if (errors.Count > 0)
        {
            return AuthResult.Fail(AuthErrorKind.Validation, "New password does not meet the requirements.", errors);
        }

        await _userService.ChangePasswordAsync(user, request.NewPassword, cancellationToken);
        return await IssueTokenAsync(user, currentToken.FamilyId, cancellationToken);
    }

    private async Task<AuthResult> HandleReuseAsync(RefreshToken storedToken, CancellationToken cancellationToken)
    {
        await _refreshTokenService.RevokeFamilyAsync(storedToken.FamilyId, cancellationToken: cancellationToken);
        var hasOtherSessions = await _refreshTokenService.HasActiveSessionsAsync(storedToken.UserId, cancellationToken);

        await _notificationPublisher.PublishTokenReuseDetectedAsync(
            storedToken.UserId, storedToken.FamilyId, DateTime.UtcNow, hasOtherSessions, cancellationToken);

        var warning = hasOtherSessions
            ? "A reused refresh token was detected and this session was terminated. Your other sessions have been alerted."
            : "A reused refresh token was detected and this session was terminated. You have no other active sessions; please log in again and consider changing your password.";

        return AuthResult.Fail(AuthErrorKind.Unauthorized, "Refresh token reuse detected. Session revoked.", warning: warning);
    }
}

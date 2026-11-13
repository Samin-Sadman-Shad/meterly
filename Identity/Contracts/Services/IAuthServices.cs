using Identity.DTOs;
using Identity.Model;
using Microsoft.AspNetCore.Identity;

namespace Identity.Contracts.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> RefreshTokenAsync(string? incomingRefreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? incomingRefreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RevokeSessionsByFamilyIdAsync(Guid userId, Guid familyId, CancellationToken cancellationToken = default);
    Task<AuthResult> ChangePasswordAsync(Guid userId, string? incomingRefreshToken, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> IssueTokenAsync(User user, Guid familyId, CancellationToken cancellationToken = default);
}

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(User? User, AuthError? Error)> RegisterUserAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<User?> ValidateCredentialsAsync(string identifier, string password, CancellationToken cancellationToken = default);
    Task<bool> VerifyPasswordAsync(User user, string password, CancellationToken cancellationToken = default);
    IReadOnlyList<string> ValidateNewPassword(string newPassword, string? currentPassword = null);
    Task ChangePasswordAsync(User user, string newPassword, CancellationToken cancellationToken = default);
}

public interface IRefreshTokenService
{
    Task<RefreshToken?> GetByTokenAsync(string plainToken, CancellationToken cancellationToken = default);

    /// <summary>Invalidates the family's unused tokens and issues a new one under the same family.</summary>
    Task<IssuedRefreshToken> RotateAsync(Guid userId, Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>Returns false if a concurrent request consumed the token first.</summary>
    Task<bool> MarkAsUsedAsync(RefreshToken token, CancellationToken cancellationToken = default);

    Task<bool> HasActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> RevokeFamilyAsync(Guid familyId, Guid? userId = null, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IAccessTokenService
{
    string GenerateAccessToken(User user);
}

public interface IPasswordHasherAdapter<TUser> where TUser : class
{
    string HashPassword(TUser user, string plainPassword);
    PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword);
}

public interface ISecurityNotificationPublisher
{
    Task PublishTokenReuseDetectedAsync(
        Guid userId,
        Guid familyId,
        DateTime detectedAtUtc,
        bool hasOtherActiveSessions,
        CancellationToken cancellationToken = default);
}

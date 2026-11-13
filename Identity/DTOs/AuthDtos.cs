using System.ComponentModel.DataAnnotations;

namespace Identity.DTOs;

public record RegisterRequest(
    [Required, StringLength(64, MinimumLength = 3)] string UserName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(128)] string Password,
    [Required, StringLength(100)] string FirstName,
    [Required, StringLength(100)] string LastName);

public record LoginRequest(
    [Required, StringLength(256)] string Identifier,
    [Required, StringLength(128)] string Password);

public record ChangePasswordRequest(
    [Required, StringLength(128)] string CurrentPassword,
    [Required, StringLength(128)] string NewPassword);

public record UserDataDto(Guid Id, string UserName, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles);

public record AuthResponse(string AccessToken, UserDataDto User);

public enum AuthErrorKind
{
    Unauthorized,
    Validation,
    Conflict
}

public record AuthError(AuthErrorKind Kind, string Message, IReadOnlyList<string>? Details = null);

/// <summary>Plaintext refresh token (sent to the client once, never stored) and its expiry.</summary>
public record IssuedRefreshToken(Guid FamilyId, string PlainToken, DateTime ExpiresAt);

public record AuthResult(
    bool Succeeded,
    AuthError? Error = null,
    IssuedRefreshToken? RefreshToken = null,
    string? AccessToken = null,
    UserDataDto? UserData = null,
    string? Warning = null)
{
    public static AuthResult Fail(AuthErrorKind kind, string message, IReadOnlyList<string>? details = null, string? warning = null)
        => new(false, new AuthError(kind, message, details), Warning: warning);
}

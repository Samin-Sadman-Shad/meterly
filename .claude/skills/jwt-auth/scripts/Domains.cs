

namespace JwtAuth.Reference;

// ============================================================================
// 1. DOMAIN ENTITIES
// ============================================================================

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new() { "User" };
}

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive() => !IsRevoked && !IsUsed && !IsExpired();

    public async Task RevokeAsync(Func<RefreshToken, Task>? onRevoked = null)
    {
        IsRevoked = true;
        if (onRevoked is not null)
        {
            await onRevoked(this);
        }
    }
}

// ============================================================================
// 2. DTOs & RESPONSE MODELS
// ============================================================================

public record RegisterRequest(string UserName, string Email, string Password);
public record LoginRequest(string Identifier, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record UserDataDto(Guid Id, string UserName, string Email, IReadOnlyList<string> Roles);

public record AuthResult(
    bool Succeeded,
    string? ErrorMessage = null,
    string? RefreshToken = null,
    DateTime? RefreshTokenExpiresAt = null,
    string? AccessToken = null,
    UserDataDto? UserData = null);
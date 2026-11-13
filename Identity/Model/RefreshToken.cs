namespace Identity.Model;

/// <summary>
/// A single rotating refresh token. Every login creates a new token family (one per device/session);
/// each refresh marks the presented token as used and issues a successor in the same family.
/// Only the SHA-256 hash of the token is persisted.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string TokenHash { get; set; }
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    /// <summary>Optimistic concurrency token: stops two parallel refreshes from both consuming the same token.</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive() => !IsRevoked && !IsUsed && !IsExpired();

    public void MarkUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void Revoke()
    {
        if (IsRevoked) return;
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        ConcurrencyStamp = Guid.NewGuid();
    }
}

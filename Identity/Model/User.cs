namespace Identity.Model;

/// <summary>
/// Authentication user. Roles are carried on the entity and emitted as <c>ClaimTypes.Role</c> claims,
/// so <c>[Authorize(Roles = "...")]</c> and role policies work directly off the access token.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    /// <summary>Upper-invariant copies used for case-insensitive, collation-independent lookups and unique indexes.</summary>
    public string NormalizedUserName { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new() { AppRoles.Default };
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    public void SetUserName(string userName)
    {
        UserName = userName.Trim();
        NormalizedUserName = Normalize(userName);
    }

    public void SetEmail(string email)
    {
        Email = email.Trim();
        NormalizedEmail = Normalize(email);
    }

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}

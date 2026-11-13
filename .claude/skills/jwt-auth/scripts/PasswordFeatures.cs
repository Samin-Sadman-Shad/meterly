using Microsoft.AspNetCore.Identity;

namespace JwtAuth.Reference;

public interface IPasswordHasher<TUser> where TUser : class
{
    string HashPassword(TUser user, string plainPassword);
    PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword);
}

public class IdentityPasswordHasherAdapter<TUser> : IPasswordHasher<TUser> where TUser : class
{
    private readonly PasswordHasher<TUser> _hasher = new();

    public string HashPassword(TUser user, string plainPassword)
        => _hasher.HashPassword(user, plainPassword);

    public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        => _hasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
}

public class PasswordRuleBuilder
{
    private readonly List<(Func<string, bool> Predicate, string Error)> _rules = new();

    public PasswordRuleBuilder AddRule(Func<string, bool> predicate, string errorMessage)
    {
        _rules.Add((predicate, errorMessage));
        return this;
    }

    public static PasswordRuleBuilder CreateDefault(string? currentPassword = null)
    {
        var builder = new PasswordRuleBuilder()
            .AddRule(p => !string.IsNullOrWhiteSpace(p) && p.Length >= 8, "Password must be at least 8 characters long.")
            .AddRule(p => p.Any(char.IsUpper), "Password must contain at least one uppercase letter.")
            .AddRule(p => p.Any(char.IsLower), "Password must contain at least one lowercase letter.")
            .AddRule(p => p.Any(char.IsDigit), "Password must contain at least one digit.")
            .AddRule(p => p.Any(ch => !char.IsLetterOrDigit(ch)), "Password must contain at least one special character.");

        if (!string.IsNullOrEmpty(currentPassword))
        {
            builder.AddRule(p => p != currentPassword, "New password must be different from the current password.");
        }

        return builder;
    }

    public (bool IsValid, List<string> Errors) Validate(string password)
    {
        var errors = _rules
            .Where(r => !r.Predicate(password))
            .Select(r => r.Error)
            .ToList();

        return (errors.Count == 0, errors);
    }
}
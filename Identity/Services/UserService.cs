using Identity.Contracts.Repositories;
using Identity.Contracts.Services;
using Identity.DTOs;
using Identity.Model;
using Identity.Security;
using Microsoft.AspNetCore.Identity;

namespace Identity.Services;

public class UserService : IUserService
{
    private readonly IUserRepository<User> _userRepo;
    private readonly IPasswordHasherAdapter<User> _passwordHasher;

    // Verified against when the user does not exist, so "unknown user" and "wrong password"
    // take the same time and user names cannot be enumerated by response timing.
    private readonly Lazy<string> _dummyHash;
    private static readonly User DummyUser = new() { FirstName = "-", LastName = "-" };

    public UserService(IUserRepository<User> userRepo, IPasswordHasherAdapter<User> passwordHasher)
    {
        _userRepo = userRepo;
        _passwordHasher = passwordHasher;
        _dummyHash = new Lazy<string>(() => _passwordHasher.HashPassword(DummyUser, Guid.NewGuid().ToString()));
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _userRepo.GetByIdAsync(userId, cancellationToken);

    public async Task<(User? User, AuthError? Error)> RegisterUserAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var errors = ValidateNewPassword(request.Password);
        if (errors.Count > 0)
        {
            return (null, new AuthError(AuthErrorKind.Validation, "Password does not meet the requirements.", errors));
        }

        if (await _userRepo.ExistsAsync(request.UserName, request.Email, cancellationToken))
        {
            return (null, new AuthError(AuthErrorKind.Conflict, "Username or email already exists."));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Roles = new() { AppRoles.Default }
        };
        user.SetUserName(request.UserName);
        user.SetEmail(request.Email);
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        if (!await _userRepo.AddAsync(user, cancellationToken))
        {
            return (null, new AuthError(AuthErrorKind.Conflict, "Username or email already exists."));
        }

        return (user, null);
    }

    public async Task<User?> ValidateCredentialsAsync(string identifier, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByUserNameOrEmailAsync(identifier, cancellationToken);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            _passwordHasher.VerifyHashedPassword(DummyUser, _dummyHash.Value, password);
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _userRepo.UpdateAsync(user, cancellationToken);
        }

        return user;
    }

    public async Task<bool> VerifyPasswordAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(user.PasswordHash)) return false;

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _userRepo.UpdateAsync(user, cancellationToken);
        }
        return result != PasswordVerificationResult.Failed;
    }

    public IReadOnlyList<string> ValidateNewPassword(string newPassword, string? currentPassword = null)
        => PasswordRuleBuilder.CreateDefault(currentPassword).Validate(newPassword).Errors;

    public async Task ChangePasswordAsync(User user, string newPassword, CancellationToken cancellationToken = default)
    {
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _userRepo.UpdateAsync(user, cancellationToken);
    }
}

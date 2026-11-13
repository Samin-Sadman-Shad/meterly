using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace JwtAuth.Reference;

public interface IAccessTokenService
{
    string GenerateAccessToken(User user);
}

public class JwtAccessTokenService : IAccessTokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    public JwtAccessTokenService(string secretKey, string issuer, string audience)
    {
        _secretKey = secretKey;
        _issuer = issuer;
        _audience = audience;
    }
    public string GenerateAccessToken(User user)
    {
        var claims = new List
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: creds
            );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
public interface IUserService
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<(bool Succeeded, User? User, string? Error)> RegisterUserAsync(RegisterRequest request);
    Task<User?> ValidateCredentialsAsync(string identifier, string password);
    Task<(bool Succeeded, List Errors)> VerifyAndChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
}
public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasherAdapter _passwordHasher;
    public UserService(IUserRepository userRepo, IPasswordHasherAdapter passwordHasher)
    {
        _userRepo = userRepo;
        _passwordHasher = passwordHasher;
    }
    public Task<User?> GetByIdAsync(Guid userId) => _userRepo.GetByIdAsync(userId);
    public async Task<(bool Succeeded, User? User, string? Error)> RegisterUserAsync(RegisterRequest request)
    {
        if (await _userRepo.ExistsAsync(request.UserName, request.Email))
        {
            return (false, null, "Username or email already exists.");
        }
        var (isValid, errors) = PasswordRuleBuilder.CreateDefault().Validate(request.Password);
        if (!isValid)
        {
            return (false, null, string.Join(" ", errors));
        }
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        await _userRepo.AddAsync(user);
        return (true, user, null);
    }
    public async Task<User?> ValidateCredentialsAsync(string identifier, string password)
    {
        var user = await _userRepo.GetByUserNameOrEmailAsync(identifier);
        if (user is null) return null;
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _userRepo.UpdateAsync(user);
        }
        return user;
    }
    public async Task<(bool Succeeded, List Errors)> VerifyAndChangePasswordAsync(
    Guid userId, string currentPassword, string newPassword)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
        {
            return (false, new List { "User not found." });
        }
        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return (false, new List { "Current password is incorrect." });
        }
        var (isValid, errors) = PasswordRuleBuilder.CreateDefault(currentPassword).Validate(newPassword);
        if (!isValid)
        {
            return (false, errors);
        }
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _userRepo.UpdateAsync(user);
        return (true, new List());
    }
}
public interface IRefreshTokenService
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RotateAsync(Guid userId, Guid familyId);
    Task MarkAsUsedAsync(RefreshToken token);
    Task RevokeFamilyAsync(Guid familyId);
    Task RevokeAllForUserAsync(Guid userId);
}
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _tokenRepo;
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    public RefreshTokenService(IRefreshTokenRepository tokenRepo)
    {
        _tokenRepo = tokenRepo;
    }
    public Task<RefreshToken?> GetByTokenAsync(string token)
    => _tokenRepo.GetByTokenAsync(token);
    public async Task RotateAsync(Guid userId, Guid familyId)
    {
        await _tokenRepo.InvalidatePreviousTokensAsync(userId, familyId);
        var newToken = await _tokenRepo.CreateRefreshTokenAsync(userId, familyId, RefreshTokenLifetime);
        await _tokenRepo.SaveAsync(newToken);
        return newToken;
    }
    public async Task MarkAsUsedAsync(RefreshToken token)
    {
        token.IsUsed = true;
        await _tokenRepo.UpdateAsync(token);
    }
    public Task RevokeFamilyAsync(Guid familyId)
    => _tokenRepo.RevokeTokenByFamilyAsync(familyId);
    public Task RevokeAllForUserAsync(Guid userId)
    => _tokenRepo.RevokeTokenByUserIdAsync(userId);
}
public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request);
    Task LoginAsync(LoginRequest request);
    Task RefreshAsync(string? incomingRefreshToken);
    Task LogoutAsync(string? incomingRefreshToken);
    Task RevokeAllSessionsAsync(Guid userId);
    Task ChangePasswordAsync(Guid userId, string? incomingRefreshToken, ChangePasswordRequest request);
    Task IssueTokenAsync(User user, Guid familyId);
}
public class AuthService : IAuthService
{
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
    public async Task IssueTokenAsync(User user, Guid familyId)
    {
        var refreshToken = await _refreshTokenService.RotateAsync(user.Id, familyId);
        var accessToken = _accessTokenService.GenerateAccessToken(user);
        var userData = new UserDataDto(user.Id, user.UserName, user.Email, user.Roles);
        return new AuthResult(
        Succeeded: true,
        RefreshToken: refreshToken.Token,
        RefreshTokenExpiresAt: refreshToken.ExpiresAt,
        AccessToken: accessToken,
        UserData: userData);
    }
    public async Task RegisterAsync(RegisterRequest request)
    {
        var (succeeded, user, error) = await _userService.RegisterUserAsync(request);
        if (!succeeded || user is null)
        {
            return new AuthResult(false, ErrorMessage: error);
        }
        var familyId = Guid.NewGuid();
        return await IssueTokenAsync(user, familyId);
    }
    public async Task LoginAsync(LoginRequest request)
    {
        var user = await _userService.ValidateCredentialsAsync(request.Identifier, request.Password);
        if (user is null)
        {
            return new AuthResult(false, ErrorMessage: "Invalid credentials.");
        }
        var familyId = Guid.NewGuid();
        return await IssueTokenAsync(user, familyId);
    }
    public async Task RefreshAsync(string? incomingRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(incomingRefreshToken))
        {
            return new AuthResult(false, ErrorMessage: "Refresh token is missing.");
        }
        var storedToken = await _refreshTokenService.GetByTokenAsync(incomingRefreshToken);
        if (storedToken is null || storedToken.IsRevoked || storedToken.IsExpired())
        {
            return new AuthResult(false, ErrorMessage: "Refresh token is invalid or expired.");
        }
        // Reuse Detection: token was already used -> revoke entire family & notify via broker
        if (storedToken.IsUsed)
        {
            await _refreshTokenService.RevokeFamilyAsync(storedToken.FamilyId);
            await _notificationPublisher.PublishTokenReuseDetectedAsync(
            storedToken.UserId,
            storedToken.FamilyId,
            DateTime.UtcNow);
            return new AuthResult(false, ErrorMessage: "Refresh token reuse detected. Session family revoked.");
        }
        var user = await _userService.GetByIdAsync(storedToken.UserId);
        if (user is null)
        {
            return new AuthResult(false, ErrorMessage: "User not found.");
        }
        await _refreshTokenService.MarkAsUsedAsync(storedToken);
        return await IssueTokenAsync(user, storedToken.FamilyId);
    }
    public async Task LogoutAsync(string? incomingRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(incomingRefreshToken)) return;
        var storedToken = await _refreshTokenService.GetByTokenAsync(incomingRefreshToken);
        if (storedToken is not null)
        {
            await _refreshTokenService.RevokeFamilyAsync(storedToken.FamilyId);
        }
    }
    public Task RevokeAllSessionsAsync(Guid userId)
    => _refreshTokenService.RevokeAllForUserAsync(userId);
    public async Task ChangePasswordAsync(
    Guid userId, string? incomingRefreshToken, ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(incomingRefreshToken))
        {
            return new AuthResult(false, ErrorMessage: "Refresh token is required.");
        }
        var storedToken = await _refreshTokenService.GetByTokenAsync(incomingRefreshToken);
        if (storedToken is null || storedToken.UserId != userId || storedToken.IsUsed || !storedToken.IsActive())
        {
            return new AuthResult(false, ErrorMessage: "Session expired or invalid refresh token.");
        }
        var (succeeded, errors) = await _userService.VerifyAndChangePasswordAsync(
        userId, request.CurrentPassword, request.NewPassword);
        if (!succeeded)
        {
            return new AuthResult(false, ErrorMessage: string.Join(" ", errors));
        }
        var user = await _userService.GetByIdAsync(userId);
        return await IssueTokenAsync(user!, storedToken.FamilyId);
    }
}
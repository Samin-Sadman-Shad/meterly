namespace JwtAuth.Reference;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "refreshToken";
    private const string RefreshEndpointPath = "/api/auth/refresh";
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    [HttpPost("register")]
    public async Task Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (!result.Succeeded) return BadRequest(new { error = result.ErrorMessage });
        AppendRefreshCookie(result.RefreshToken!, result.RefreshTokenExpiresAt!.Value);
        return Ok(new { accessToken = result.AccessToken, user = result.UserData });
    }
    [HttpPost("login")]
    public async Task Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Succeeded) return Unauthorized(new { error = result.ErrorMessage });
        AppendRefreshCookie(result.RefreshToken!, result.RefreshTokenExpiresAt!.Value);
        return Ok(new { accessToken = result.AccessToken, user = result.UserData });
    }
    [HttpPost("refresh")]
    public async Task Refresh()
    {
        Request.Cookies.TryGetValue(RefreshCookieName, out var incomingToken);
        var result = await _authService.RefreshAsync(incomingToken);
        if (!result.Succeeded)
        {
            ClearRefreshCookie();
            return Unauthorized(new { error = result.ErrorMessage });
        }
        AppendRefreshCookie(result.RefreshToken!, result.RefreshTokenExpiresAt!.Value);
        return Ok(new { accessToken = result.AccessToken, user = result.UserData });
    }
    [HttpPost("logout")]
    public async Task Logout()
    {
        Request.Cookies.TryGetValue(RefreshCookieName, out var incomingToken);
        await _authService.LogoutAsync(incomingToken);
        ClearRefreshCookie();
        return NoContent();
    }
    [Authorize]
    [HttpPost("revoke-all")]
    public async Task RevokeAll()
    {
        var userId = GetCurrentUserId();
        await _authService.RevokeAllSessionsAsync(userId);
        ClearRefreshCookie();
        return NoContent();
    }
    [Authorize]
    [HttpPost("change-password")]
    public async Task ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        Request.Cookies.TryGetValue(RefreshCookieName, out var incomingToken);
        var result = await _authService.ChangePasswordAsync(userId, incomingToken, request);
        if (!result.Succeeded)
        {
            return Unauthorized(new { error = result.ErrorMessage });
        }
        AppendRefreshCookie(result.RefreshToken!, result.RefreshTokenExpiresAt!.Value);
        return Ok(new { accessToken = result.AccessToken, user = result.UserData });
    }
    [Authorize(Roles = "Admin")]
    [HttpGet("admin-check")]
    public IActionResult AdminCheck() => Ok(new { message = "Role-based authorization verified." });
    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        return Guid.Parse(claim);
    }
    private void AppendRefreshCookie(string token, DateTime expiresAtUtc)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = RefreshEndpointPath,
            Expires = expiresAtUtc
        });
    }
    private void ClearRefreshCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = RefreshEndpointPath
        });
    }
}
using Identity.Contracts.Services;
using Identity.DTOs;
using Identity.Model;
using Identity.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        => ToTokenResponse(await _authService.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => ToTokenResponse(await _authService.LoginAsync(request, cancellationToken));

    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(ReadRefreshCookie(), cancellationToken);
        if (!result.Succeeded)
        {
            ClearRefreshCookie();
        }
        return ToTokenResponse(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(ReadRefreshCookie(), cancellationToken);
        ClearRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
    {
        await _authService.RevokeAllSessionsAsync(GetCurrentUserId(), cancellationToken);
        ClearRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        => ToTokenResponse(await _authService.ChangePasswordAsync(GetCurrentUserId(), ReadRefreshCookie(), request, cancellationToken));

    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpGet("admin-check")]
    public IActionResult AdminCheck() => Ok(new { message = "Role-based authorization verified." });

    private IActionResult ToTokenResponse(AuthResult result)
    {
        if (result.Succeeded)
        {
            AppendRefreshCookie(result.RefreshToken!);
            return Ok(new AuthResponse(result.AccessToken!, result.UserData!));
        }

        var error = result.Error!;
        var body = new { error = error.Message, details = error.Details, warning = result.Warning };
        return error.Kind switch
        {
            AuthErrorKind.Validation => BadRequest(body),
            AuthErrorKind.Conflict => Conflict(body),
            _ => Unauthorized(body)
        };
    }

    private Guid GetCurrentUserId()
    {
        // [Authorize] guarantees an authenticated principal; a missing/garbled claim means a token we did not issue.
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException("Missing user identifier claim.");
    }

    private string? ReadRefreshCookie()
        => Request.Cookies.TryGetValue(RefreshCookie.Name, out var token) ? token : null;

    private void AppendRefreshCookie(IssuedRefreshToken token)
        => Response.Cookies.Append(RefreshCookie.Name, token.PlainToken, BuildCookieOptions(token.ExpiresAt));

    private void ClearRefreshCookie()
        => Response.Cookies.Delete(RefreshCookie.Name, BuildCookieOptions(expiresAt: null));

    private static CookieOptions BuildCookieOptions(DateTime? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = RefreshCookie.SameSite,
        Path = RefreshCookie.Path,
        Expires = expiresAt,
        IsEssential = true
    };
}

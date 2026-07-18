using Auth.Application.Authentication.Login;
using Auth.Application.Authentication.Logout;
using Auth.Application.Authentication.Refresh;
using Auth.Application.Authentication.Register;
using Auth.Application.Authentication.RevokeAll;
using Auth.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly RegisterHandler _registerHandler;
    private readonly LoginHandler _loginHandler;
    private readonly RefreshTokenHandler _refreshHandler;
    private readonly LogoutHandler _logoutHandler;
    private readonly RevokeAllHandler _revokeAllHandler;

    public AuthenticationController(
        RegisterHandler registerHandler,
        LoginHandler loginHandler,
        RefreshTokenHandler refreshHandler,
        LogoutHandler logoutHandler,
        RevokeAllHandler revokeAllHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
        _logoutHandler = logoutHandler;
        _revokeAllHandler = revokeAllHandler;
    }

    [HttpPost("register")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        await _registerHandler.HandleAsync(request, cancellationToken);
        return Ok(new { Message = "If the email is registered and unverified, a verification link has been printed to the service logs." });
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _loginHandler.HandleAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _refreshHandler.HandleAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _logoutHandler.HandleAsync(request.RefreshToken, ipAddress, cancellationToken);
        return Ok(new { Message = "Logged out successfully." });
    }

    [Authorize]
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        await _revokeAllHandler.HandleAsync(userGuid, cancellationToken);
        return Ok(new { Message = "All active sessions revoked successfully." });
    }
}

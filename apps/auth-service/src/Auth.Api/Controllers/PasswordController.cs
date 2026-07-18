using Auth.Application.Password.ChangePassword;
using Auth.Application.Password.ForgotPassword;
using Auth.Application.Password.ResetPassword;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/auth/password")]
public class PasswordController : ControllerBase
{
    private readonly ChangePasswordHandler _changeHandler;
    private readonly ForgotPasswordHandler _forgotHandler;
    private readonly ResetPasswordHandler _resetHandler;

    public PasswordController(
        ChangePasswordHandler changeHandler,
        ForgotPasswordHandler forgotHandler,
        ResetPasswordHandler resetHandler)
    {
        _changeHandler = changeHandler;
        _forgotHandler = forgotHandler;
        _resetHandler = resetHandler;
    }

    [Authorize]
    [HttpPut("change")]
    public async Task<IActionResult> Change([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        await _changeHandler.HandleAsync(userGuid, request, cancellationToken);
        return Ok(new { Message = "Password changed successfully." });
    }

    [HttpPost("forgot")]
    public async Task<IActionResult> Forgot([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _forgotHandler.HandleAsync(request, cancellationToken);
        return Ok(new { Message = "If the email is registered, a password reset link has been printed to the service logs." });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _resetHandler.HandleAsync(request, cancellationToken);
        return Ok(new { Message = "Password reset successfully. You can now log in." });
    }
}

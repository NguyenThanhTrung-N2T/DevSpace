using Auth.Application.EmailVerification.ConfirmEmail;
using Auth.Application.EmailVerification.ResendEmail;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/auth/verification")]
public class VerificationController : ControllerBase
{
    private readonly ConfirmEmailHandler _confirmHandler;
    private readonly ResendEmailHandler _resendHandler;

    public VerificationController(ConfirmEmailHandler confirmHandler, ResendEmailHandler resendHandler)
    {
        _confirmHandler = confirmHandler;
        _resendHandler = resendHandler;
    }

    [HttpGet("confirm")]
    public async Task<IActionResult> Confirm([FromQuery] string userId, [FromQuery] string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return BadRequest("Invalid user ID or token.");
        }

        await _confirmHandler.HandleAsync(userGuid, token, cancellationToken);
        return Ok("Email confirmed successfully. You can now log in.");
    }

    [HttpPost("resend")]
    public async Task<IActionResult> Resend([FromBody] ResendEmailRequest request, CancellationToken cancellationToken)
    {
        await _resendHandler.HandleAsync(request, cancellationToken);
        return Ok(new { Message = "If the email is registered and unverified, a verification link has been printed to the service logs." });
    }
}

using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Options;
using Microsoft.Extensions.Options;
using System.Net;

namespace Auth.Application.Password.ForgotPassword;

public class ForgotPasswordHandler
{
    private readonly IUserService _userService;
    private readonly IPasswordService _passwordService;
    private readonly IEmailSender _emailSender;
    private readonly SecurityOptions _securityOptions;

    public ForgotPasswordHandler(
        IUserService userService, 
        IPasswordService passwordService, 
        IEmailSender emailSender,
        IOptions<SecurityOptions> securityOptions)
    {
        _userService = userService;
        _passwordService = passwordService;
        _emailSender = emailSender;
        _securityOptions = securityOptions.Value;
    }

    public async Task HandleAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userService.FindByEmailAsync(request.Email, cancellationToken);
        if (user != null && user.IsActive)
        {
            var token = await _passwordService.GeneratePasswordResetTokenAsync(user.Id, cancellationToken);
            var baseOrigin = _securityOptions.PublicOrigin.TrimEnd('/');
            var resetLink = $"{baseOrigin}/api/auth/password/reset?userId={user.Id}&token={WebUtility.UrlEncode(token)}";

            await _emailSender.SendEmailAsync(
                user.Email,
                "Reset Password Request",
                $"Please click the following link to reset your password: {resetLink}"
            );
        }
    }
}

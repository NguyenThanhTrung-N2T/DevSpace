using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Options;
using FluentValidation;
using Microsoft.Extensions.Options;
using System.Net;

namespace Auth.Application.Password.ForgotPassword;

public class ForgotPasswordHandler
{
    private readonly IUserService _userService;
    private readonly IPasswordService _passwordService;
    private readonly IEmailSender _emailSender;
    private readonly SecurityOptions _securityOptions;
    private readonly IValidator<ForgotPasswordRequest> _validator;

    public ForgotPasswordHandler(
        IUserService userService, 
        IPasswordService passwordService, 
        IEmailSender emailSender,
        IOptions<SecurityOptions> securityOptions,
        IValidator<ForgotPasswordRequest> validator)
    {
        _userService = userService;
        _passwordService = passwordService;
        _emailSender = emailSender;
        _securityOptions = securityOptions.Value;
        _validator = validator;
    }

    public async Task HandleAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Auth.Application.Common.Exceptions.ValidationException(validationResult.Errors);
        }

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

using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Options;
using FluentValidation;
using Microsoft.Extensions.Options;
using System.Net;

namespace Auth.Application.EmailVerification.ResendEmail;

public class ResendEmailHandler
{
    private readonly IUserService _userService;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IEmailSender _emailSender;
    private readonly SecurityOptions _securityOptions;
    private readonly IValidator<ResendEmailRequest> _validator;

    public ResendEmailHandler(
        IUserService userService, 
        IEmailVerificationService emailVerificationService, 
        IEmailSender emailSender,
        IOptions<SecurityOptions> securityOptions,
        IValidator<ResendEmailRequest> validator)
    {
        _userService = userService;
        _emailVerificationService = emailVerificationService;
        _emailSender = emailSender;
        _securityOptions = securityOptions.Value;
        _validator = validator;
    }

    public async Task HandleAsync(ResendEmailRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Auth.Application.Common.Exceptions.ValidationException(validationResult.Errors);
        }

        var user = await _userService.FindByEmailAsync(request.Email, cancellationToken);
        
        // Return generic success to avoid user enumeration if user does not exist or is inactive
        if (user == null || !user.IsActive)
        {
            return;
        }

        if (user.EmailConfirmed)
        {
            throw new ConflictException("Email address is already verified.");
        }

        var emailToken = await _emailVerificationService.GenerateEmailConfirmationTokenAsync(user.Id, cancellationToken);
        var baseOrigin = _securityOptions.PublicOrigin.TrimEnd('/');
        var verificationLink = $"{baseOrigin}/api/auth/verification/confirm?userId={user.Id}&token={WebUtility.UrlEncode(emailToken)}";

        await _emailSender.SendEmailAsync(
            user.Email,
            "Re-sent Email Verification",
            $"Please click the following link to confirm your registration: {verificationLink}"
        );
    }
}

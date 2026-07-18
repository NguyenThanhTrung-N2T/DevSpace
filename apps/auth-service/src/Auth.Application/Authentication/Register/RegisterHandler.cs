using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Options;
using FluentValidation;
using Microsoft.Extensions.Options;
using System.Net;

namespace Auth.Application.Authentication.Register;

public class RegisterHandler
{
    private readonly IUserService _userService;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IEmailSender _emailSender;
    private readonly SecurityOptions _securityOptions;
    private readonly IValidator<RegisterRequest> _validator;

    public RegisterHandler(
        IUserService userService, 
        IEmailVerificationService emailVerificationService, 
        IEmailSender emailSender,
        IOptions<SecurityOptions> securityOptions,
        IValidator<RegisterRequest> validator)
    {
        _userService = userService;
        _emailVerificationService = emailVerificationService;
        _emailSender = emailSender;
        _securityOptions = securityOptions.Value;
        _validator = validator;
    }

    public async Task HandleAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Auth.Application.Common.Exceptions.ValidationException(validationResult.Errors);
        }

        var existingUser = await _userService.FindByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new ConflictException("Email already exists.");
        }

        var user = await _userService.CreateUserAsync(request.Email, request.DisplayName, request.Password, cancellationToken);

        // Generate verification link and send
        var token = await _emailVerificationService.GenerateEmailConfirmationTokenAsync(user.Id, cancellationToken);
        var baseOrigin = _securityOptions.PublicOrigin.TrimEnd('/');
        var verificationLink = $"{baseOrigin}/api/auth/verification/confirm?userId={user.Id}&token={WebUtility.UrlEncode(token)}";

        await _emailSender.SendEmailAsync(
            user.Email, 
            "Confirm Registration", 
            $"Please click the following link to confirm your registration: {verificationLink}"
        );
    }
}

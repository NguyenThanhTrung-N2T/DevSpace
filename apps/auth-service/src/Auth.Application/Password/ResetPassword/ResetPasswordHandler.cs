using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using FluentValidation;
using FluentValidation.Results;

namespace Auth.Application.Password.ResetPassword;

public class ResetPasswordHandler
{
    private readonly IUserService _userService;
    private readonly IPasswordService _passwordService;
    private readonly IValidator<ResetPasswordRequest> _validator;

    public ResetPasswordHandler(
        IUserService userService, 
        IPasswordService passwordService,
        IValidator<ResetPasswordRequest> validator)
    {
        _userService = userService;
        _passwordService = passwordService;
        _validator = validator;
    }

    public async Task HandleAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Auth.Application.Common.Exceptions.ValidationException(validationResult.Errors);
        }

        var user = await _userService.FindByIdAsync(request.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new Auth.Application.Common.Exceptions.ValidationException(new[]
            {
                new ValidationFailure("UserId", "Invalid user or request.")
            });
        }

        await _passwordService.ResetPasswordAsync(user.Id, request.Token, request.NewPassword, cancellationToken);
    }
}

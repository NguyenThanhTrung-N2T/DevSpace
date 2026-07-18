using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using FluentValidation;

namespace Auth.Application.Password.ChangePassword;

public class ChangePasswordHandler
{
    private readonly IPasswordService _passwordService;
    private readonly IValidator<ChangePasswordRequest> _validator;

    public ChangePasswordHandler(IPasswordService passwordService, IValidator<ChangePasswordRequest> validator)
    {
        _passwordService = passwordService;
        _validator = validator;
    }

    public async Task HandleAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Auth.Application.Common.Exceptions.ValidationException(validationResult.Errors);
        }

        await _passwordService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword, cancellationToken);
    }
}

using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using FluentValidation.Results;

namespace Auth.Application.Password.ResetPassword;

public class ResetPasswordHandler
{
    private readonly IUserService _userService;
    private readonly IPasswordService _passwordService;

    public ResetPasswordHandler(IUserService userService, IPasswordService passwordService)
    {
        _userService = userService;
        _passwordService = passwordService;
    }

    public async Task HandleAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userService.FindByIdAsync(request.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("UserId", "Invalid user or request.")
            });
        }

        await _passwordService.ResetPasswordAsync(user.Id, request.Token, request.NewPassword, cancellationToken);
    }
}

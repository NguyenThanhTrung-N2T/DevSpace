using Auth.Application.Common.Interfaces;

namespace Auth.Application.Password.ChangePassword;

public class ChangePasswordHandler
{
    private readonly IPasswordService _passwordService;

    public ChangePasswordHandler(IPasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    public async Task HandleAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        await _passwordService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword, cancellationToken);
    }
}

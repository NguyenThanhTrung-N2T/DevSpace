using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;

namespace Auth.Application.EmailVerification.ConfirmEmail;

public class ConfirmEmailHandler
{
    private readonly IUserService _userService;
    private readonly IEmailVerificationService _emailVerificationService;

    public ConfirmEmailHandler(IUserService userService, IEmailVerificationService emailVerificationService)
    {
        _userService = userService;
        _emailVerificationService = emailVerificationService;
    }

    public async Task HandleAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await _userService.FindByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new NotFoundException("User not found.");
        }

        await _emailVerificationService.ConfirmEmailAsync(user.Id, token, cancellationToken);
    }
}

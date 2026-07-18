using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;

namespace Auth.Application.Authentication.GetMe;

public class GetMeHandler
{
    private readonly IUserService _userService;

    public GetMeHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<UserDto> HandleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userService.FindByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("User is inactive or deleted.");
        }

        return new UserDto(user.Id, user.Email, user.DisplayName, user.AvatarUrl);
    }
}

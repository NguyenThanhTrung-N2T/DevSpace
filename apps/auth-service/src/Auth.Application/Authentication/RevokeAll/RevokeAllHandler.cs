using Auth.Application.Common.Interfaces;

namespace Auth.Application.Authentication.RevokeAll;

public class RevokeAllHandler
{
    private readonly IRefreshTokenService _refreshTokenService;

    public RevokeAllHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task HandleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _refreshTokenService.RevokeAllUserTokensAsync(userId, cancellationToken);
    }
}

using Auth.Application.Common.Interfaces;

namespace Auth.Application.Authentication.Logout;

public class LogoutHandler
{
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task HandleAsync(string rawRefreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        await _refreshTokenService.RevokeTokenAsync(rawRefreshToken, "User logged out", ipAddress, cancellationToken);
    }
}

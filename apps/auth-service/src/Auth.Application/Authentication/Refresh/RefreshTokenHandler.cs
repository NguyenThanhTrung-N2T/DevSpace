using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;

namespace Auth.Application.Authentication.Refresh;

public class RefreshTokenHandler
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtService _jwtService;

    public RefreshTokenHandler(IRefreshTokenService refreshTokenService, IJwtService jwtService)
    {
        _refreshTokenService = refreshTokenService;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> HandleAsync(RefreshTokenRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var rotateResult = await _refreshTokenService.RotateTokenAsync(request.RefreshToken, ipAddress, userAgent, cancellationToken);
        var accessToken = _jwtService.GenerateAccessToken(rotateResult.User);

        var userDto = new UserDto(
            rotateResult.User.Id,
            rotateResult.User.Email,
            rotateResult.User.DisplayName,
            null
        );

        var expiresInSeconds = (int)(accessToken.ExpiresAt - DateTime.UtcNow).TotalSeconds;

        return new AuthResponse(
            accessToken.Token,
            expiresInSeconds,
            rotateResult.RawToken,
            userDto
        );
    }
}

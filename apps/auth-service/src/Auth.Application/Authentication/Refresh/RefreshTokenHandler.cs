using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using FluentValidation;

namespace Auth.Application.Authentication.Refresh;

public class RefreshTokenHandler
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtService _jwtService;
    private readonly IValidator<RefreshTokenRequest> _validator;

    public RefreshTokenHandler(
        IRefreshTokenService refreshTokenService, 
        IJwtService jwtService,
        IValidator<RefreshTokenRequest> validator)
    {
        _refreshTokenService = refreshTokenService;
        _jwtService = jwtService;
        _validator = validator;
    }

    public async Task<AuthResponse> HandleAsync(RefreshTokenRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Auth.Application.Common.Exceptions.ValidationException(validationResult.Errors);
        }

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

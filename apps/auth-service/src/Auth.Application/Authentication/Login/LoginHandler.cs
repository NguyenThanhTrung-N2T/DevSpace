using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;

namespace Auth.Application.Authentication.Login;

public class LoginHandler
{
    private readonly IUserService _userService;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginHandler(
        IUserService userService, 
        IPasswordService passwordService, 
        IJwtService jwtService, 
        IRefreshTokenService refreshTokenService)
    {
        _userService = userService;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthResponse> HandleAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var user = await _userService.FindByEmailAsync(request.Email, cancellationToken);
        if (user == null || !user.IsActive)
        {
            // Run a dummy PBKDF2 hash verification to prevent username enumeration via timing side-channel attacks
            await _passwordService.RunDummyHashCheckAsync(cancellationToken);
            throw new UnauthorizedException("Invalid credentials.");
        }

        // NOTE: We intentionally allow unverified emails (EmailConfirmed == false) to login
        // but restrict access to specific endpoints using the "RequireVerifiedEmail" policy.
        var isPasswordValid = await _passwordService.CheckPasswordAsync(user.Id, request.Password, lockoutOnFailure: true, cancellationToken);
        
        // NOTE: If the account is locked out, we deliberately return the same generic "Invalid credentials"
        // message to avoid leaking account status/existence (security-first over UX).
        if (!isPasswordValid)
        {
            throw new UnauthorizedException("Invalid credentials.");
        }

        // Update Last Login timestamp
        await _userService.UpdateLastLoginAsync(user.Id, cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = await _refreshTokenService.CreateTokenAsync(user.Id, ipAddress, userAgent, cancellationToken);

        var userDto = new UserDto(user.Id, user.Email, user.DisplayName, null); // AvatarUrl is not stored/required in UserInfo or can be mapped in future
        var expiresInSeconds = (int)(accessToken.ExpiresAt - DateTime.UtcNow).TotalSeconds;

        return new AuthResponse(accessToken.Token, expiresInSeconds, refreshToken.RawToken, userDto);
    }
}

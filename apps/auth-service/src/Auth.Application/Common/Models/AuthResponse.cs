namespace Auth.Application.Common.Models;

public record UserDto(string Id, string Email, string DisplayName, string? AvatarUrl);

public record AuthResponse(string AccessToken, int ExpiresIn, string RefreshToken, UserDto User);

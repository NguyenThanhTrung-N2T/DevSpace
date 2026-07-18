using Auth.Application.Common.Models;

namespace Auth.Application.Common.Interfaces;

public sealed record RefreshTokenResult(string RawToken, Guid TokenId, DateTime ExpiresAt, UserInfo User);

public interface IRefreshTokenService
{
    Task<RefreshTokenResult> CreateTokenAsync(Guid userId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<RefreshTokenResult> RotateTokenAsync(string rawRefreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(string rawRefreshToken, string? reason, string? ipAddress, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}

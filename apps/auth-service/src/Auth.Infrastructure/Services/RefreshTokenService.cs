using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Infrastructure.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IAuthDbContext _dbContext;
    private readonly UserManager<User> _userManager;

    public RefreshTokenService(IAuthDbContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    private static string GenerateRawToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); // Secure 64-character hex token
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public async Task<RefreshTokenResult> CreateTokenAsync(Guid userId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedException("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var userInfo = new UserInfo(user.Id, user.Email!, user.DisplayName, user.AvatarUrl, user.EmailConfirmed, user.IsActive, roles.ToList());

        var rawToken = GenerateRawToken();
        var hashedToken = HashToken(rawToken);
        var tokenId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var tokenEntity = new RefreshToken
        {
            Id = tokenId,
            UserId = userId,
            FamilyId = Guid.NewGuid(), // Start a new token family
            TokenHash = hashedToken,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress ?? "unknown",
            Device = userAgent ?? "unknown",
            IsRevoked = false
        };

        _dbContext.RefreshTokens.Add(tokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResult(rawToken, tokenId, expiresAt, userInfo);
    }

    public async Task<RefreshTokenResult> RotateTokenAsync(string rawRefreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        var hashedInputToken = HashToken(rawRefreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hashedInputToken, cancellationToken);

        if (storedToken == null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        // REUSE & EXPIRATION DETECTION
        if (storedToken.IsRevoked || storedToken.RevokedAt != null || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            if (storedToken.IsRevoked || storedToken.RevokedAt != null)
            {
                // Breach detected! Revoke the entire family
                var familyTokens = await _dbContext.RefreshTokens
                    .Where(t => t.FamilyId == storedToken.FamilyId && !t.IsRevoked && t.RevokedAt == null)
                    .ToListAsync(cancellationToken);

                foreach (var token in familyTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                    token.Reason = $"Revoked due to detected reuse of token {storedToken.Id}";
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            throw new UnauthorizedException("Refresh token expired or invalid.");
        }

        var user = storedToken.User;
        if (user == null || !user.IsActive || user.DeletedAt != null)
        {
            throw new UnauthorizedException("Account is inactive or deleted.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var userInfo = new UserInfo(user.Id, user.Email!, user.DisplayName, user.AvatarUrl, user.EmailConfirmed, user.IsActive, roles.ToList());

        // Revoke the old token
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.UsedAt = DateTime.UtcNow;
        storedToken.Reason = "Replaced by token rotation";

        // Generate new token in the same family
        var newRawToken = GenerateRawToken();
        var newHashedToken = HashToken(newRawToken);
        var newRefreshTokenId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = newRefreshTokenId,
            UserId = user.Id,
            FamilyId = storedToken.FamilyId, // Maintain the same family ID
            TokenHash = newHashedToken,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress ?? "unknown",
            Device = userAgent ?? "unknown",
            IsRevoked = false
        };

        // Link the old token to the replacement
        storedToken.ReplacedByTokenId = newRefreshTokenId;

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResult(newRawToken, newRefreshTokenId, expiresAt, userInfo);
    }

    public async Task RevokeTokenAsync(string rawRefreshToken, string? reason, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var hashedInputToken = HashToken(rawRefreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hashedInputToken, cancellationToken);

        if (storedToken != null && !storedToken.IsRevoked && storedToken.RevokedAt == null)
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.Reason = reason ?? "Revoked by user logout";
            if (ipAddress != null)
            {
                storedToken.CreatedByIp = ipAddress;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.Reason = "Revoked by user request (revoke all)";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    private readonly UserManager<User> _userManager;
    private readonly IAuthDbContext _dbContext;

    public PasswordService(UserManager<User> userManager, IAuthDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, bool lockoutOnFailure, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return false;

        // Check if user is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedException("Invalid credentials.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);

        if (lockoutOnFailure)
        {
            if (!isPasswordValid)
            {
                await _userManager.AccessFailedAsync(user);
            }
            else
            {
                await _userManager.ResetAccessFailedCountAsync(user);
            }
        }

        return isPasswordValid;
    }

    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var failures = result.Errors.Select(e => new FluentValidation.Results.ValidationFailure(e.Code, e.Description));
            throw new ValidationException(failures);
        }
    }

    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetToken = new UserVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = UserVerificationTokenType.PasswordReset,
            TokenHash = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserVerificationTokens.Add(resetToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var hashedToken = HashToken(token);
        var dbToken = await _dbContext.UserVerificationTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id && t.TokenHash == hashedToken && t.Type == UserVerificationTokenType.PasswordReset, cancellationToken);

        if (dbToken == null || dbToken.UsedAt != null || dbToken.ExpiresAt < DateTime.UtcNow)
        {
            var failure = new FluentValidation.Results.ValidationFailure("Token", "Invalid or expired token.");
            throw new ValidationException(new[] { failure });
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var failures = result.Errors.Select(e => new FluentValidation.Results.ValidationFailure(e.Code, e.Description));
            throw new ValidationException(failures);
        }

        dbToken.UsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RunDummyHashCheckAsync(CancellationToken cancellationToken = default)
    {
        // Run dummy hash computation using the exact same PBKDF2 algorithm to prevent timing attacks
        var hasher = new PasswordHasher<User>();
        var dummyUser = new User { Id = Guid.Empty };
        hasher.VerifyHashedPassword(dummyUser, "AQAAAAIAAYagAAAAEJF1b2R3eDR1...", "dummy_password");
        await Task.CompletedTask;
    }
}

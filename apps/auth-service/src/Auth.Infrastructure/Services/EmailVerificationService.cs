using Auth.Application.Common.Exceptions;
using Auth.Application.Common.Interfaces;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Infrastructure.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly UserManager<User> _userManager;
    private readonly IAuthDbContext _dbContext;

    public EmailVerificationService(UserManager<User> userManager, IAuthDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var verificationToken = new UserVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = UserVerificationTokenType.EmailVerification,
            TokenHash = HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserVerificationTokens.Add(verificationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        // Validate token from database verification tokens first to check for usage/expiration
        var hashedToken = HashToken(token);
        var dbToken = await _dbContext.UserVerificationTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id && t.TokenHash == hashedToken && t.Type == UserVerificationTokenType.EmailVerification, cancellationToken);

        if (dbToken == null || dbToken.UsedAt != null || dbToken.ExpiresAt < DateTime.UtcNow)
        {
            var failure = new FluentValidation.Results.ValidationFailure("Token", "Invalid or expired verification token.");
            throw new ValidationException(new[] { failure });
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            var failures = result.Errors.Select(e => new FluentValidation.Results.ValidationFailure(e.Code, e.Description));
            throw new ValidationException(failures);
        }

        dbToken.UsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

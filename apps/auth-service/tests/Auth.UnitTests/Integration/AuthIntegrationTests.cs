using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Auth.UnitTests.Integration;

public class AuthIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AuthDbContext> _contextOptions;

    public AuthIntegrationTests()
    {
        // Initialize SQLite in-memory connection
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create the database schema
        using var context = new AuthDbContext(_contextOptions);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task Should_Persist_User_And_VerificationToken()
    {
        using var context = new AuthDbContext(_contextOptions);

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test@devspace.com",
            Email = "test@devspace.com",
            DisplayName = "Test User",
            IsActive = true
        };
        context.Users.Add(user);

        var token = new UserVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = UserVerificationTokenType.EmailVerification,
            TokenHash = "somehash",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        context.UserVerificationTokens.Add(token);
        await context.SaveChangesAsync();

        // Read back user and verify relationships
        var savedUser = await context.Users
            .Include(u => u.UserVerificationTokens)
            .FirstOrDefaultAsync(u => u.Email == "test@devspace.com");

        Assert.NotNull(savedUser);
        Assert.Single(savedUser.UserVerificationTokens);
        Assert.Equal("somehash", savedUser.UserVerificationTokens.First().TokenHash);
    }

    [Fact]
    public async Task Should_Support_RefreshToken_Family_Rotation_And_Reuse_Detection()
    {
        using var context = new AuthDbContext(_contextOptions);

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "rotation@devspace.com",
            Email = "rotation@devspace.com",
            DisplayName = "Rotation User",
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var familyId = Guid.NewGuid();

        // 1. Persist initial token (Token 1)
        var token1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = "hash1",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        context.RefreshTokens.Add(token1);
        await context.SaveChangesAsync();

        // 2. Perform rotation: rotate Token 1 -> Token 2 (marks Token 1 as used/revoked)
        token1.IsRevoked = true;
        token1.RevokedAt = DateTime.UtcNow;
        token1.Reason = "Replaced by rotation";

        var token2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = "hash2",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        context.RefreshTokens.Add(token2);
        token1.ReplacedByTokenId = token2.Id;
        await context.SaveChangesAsync();

        // 3. Simulate Reuse Detection: client attempts to reuse Token 1 again
        var reuseAttemptToken = await context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == "hash1");
        Assert.NotNull(reuseAttemptToken);
        Assert.True(reuseAttemptToken.IsRevoked);

        // When reuse is detected, find and revoke all active tokens in the same family:
        var activeFamilyTokens = await context.RefreshTokens
            .Where(t => t.FamilyId == reuseAttemptToken.FamilyId && !t.IsRevoked)
            .ToListAsync();

        foreach (var t in activeFamilyTokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = DateTime.UtcNow;
            t.Reason = "Revoked due to reuse detection";
        }
        await context.SaveChangesAsync();

        // 4. Assert Token 2 was automatically revoked
        var updatedToken2 = await context.RefreshTokens.FindAsync(token2.Id);
        Assert.NotNull(updatedToken2);
        Assert.True(updatedToken2.IsRevoked);
        Assert.Equal("Revoked due to reuse detection", updatedToken2.Reason);
    }
}

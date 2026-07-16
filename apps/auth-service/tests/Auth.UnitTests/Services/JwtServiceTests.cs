using Auth.Application.Common.Options;
using Auth.Domain.Entities;
using Auth.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Auth.UnitTests.Services;

public class JwtServiceTests
{
    private readonly IOptions<JwtOptions> _jwtOptions;

    public JwtServiceTests()
    {
        // Use default JwtOptions (empty private/public keys to test fallback in-memory key generation)
        _jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7,
            KeyId = "test-key-id"
        });
    }

    [Fact]
    public void Should_Fallback_To_InMemory_Keys_When_Keys_Are_Empty()
    {
        var service = new JwtService(_jwtOptions, NullLogger<JwtService>.Instance);

        Assert.NotNull(service.GetPublicKey());
        Assert.Contains("test-key-id", service.GetJwksJson());
    }

    [Fact]
    public void Should_Generate_Valid_AccessToken()
    {
        var service = new JwtService(_jwtOptions, NullLogger<JwtService>.Instance);
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@devspace.com",
            DisplayName = "Test User"
        };
        var roles = new List<string> { "User" };

        var (token, tokenId, expiresAt) = service.GenerateAccessToken(user, roles);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.False(string.IsNullOrWhiteSpace(tokenId));
        Assert.True(expiresAt > DateTime.UtcNow);

        // Validate structure using handler
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("test-issuer", jwtToken.Issuer);
        Assert.Equal("test-audience", jwtToken.Audiences.First());
        Assert.Equal("test-key-id", jwtToken.Header.Kid);
    }

    [Fact]
    public void Should_Generate_Unique_RefreshToken()
    {
        var service = new JwtService(_jwtOptions, NullLogger<JwtService>.Instance);
        var userId = Guid.NewGuid().ToString();

        var (token1, id1, familyId1) = service.GenerateRefreshToken(userId);
        var (token2, id2, familyId2) = service.GenerateRefreshToken(userId);

        Assert.False(string.IsNullOrWhiteSpace(token1));
        Assert.False(string.IsNullOrWhiteSpace(token2));
        Assert.NotEqual(token1, token2);
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(familyId1, familyId2);
    }
}

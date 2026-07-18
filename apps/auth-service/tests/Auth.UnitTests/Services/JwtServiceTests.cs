using Auth.Application.Common.Models;
using Auth.Application.Common.Options;
using Auth.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Xunit;

namespace Auth.UnitTests.Services;

public class JwtServiceTests
{
    private readonly IOptions<JwtOptions> _jwtOptions;

    public JwtServiceTests()
    {
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
        var user = new UserInfo(
            Guid.NewGuid(),
            "test@devspace.com",
            "Test User",
            true,
            true,
            new[] { "User" }
        );

        var result = service.GenerateAccessToken(user);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.True(result.ExpiresAt > DateTime.UtcNow);

        // Validate structure using handler
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Token);

        Assert.Equal("test-issuer", jwtToken.Issuer);
        Assert.Equal("test-audience", jwtToken.Audiences.First());
        Assert.Equal("test-key-id", jwtToken.Header.Kid);
    }
}

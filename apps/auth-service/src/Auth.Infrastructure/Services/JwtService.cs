using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Options;
using Auth.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<JwtService> _logger;
    private readonly RSA _privateKey;
    private readonly RSA _publicKey;

    public JwtService(IOptions<JwtOptions> jwtOptions, ILogger<JwtService> logger)
    {
        _jwtOptions = jwtOptions.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_jwtOptions.PrivateKey) || string.IsNullOrWhiteSpace(_jwtOptions.PublicKey))
        {
            _logger.LogWarning("JWT private or public keys are not configured. Generating a temporary in-memory RSA key pair. This is NOT suitable for production!");
            var tempRsa = RSA.Create(2048);
            _privateKey = tempRsa;
            _publicKey = tempRsa;
        }
        else
        {
            try
            {
                _privateKey = LoadKey(_jwtOptions.PrivateKey);
                _publicKey = LoadKey(_jwtOptions.PublicKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configured RSA keys. Falling back to a temporary in-memory key pair.");
                var tempRsa = RSA.Create(2048);
                _privateKey = tempRsa;
                _publicKey = tempRsa;
            }
        }
    }

    public (string Token, string TokenId, DateTime ExpiresAt) GenerateAccessToken(User user, IList<string> roles)
    {
        var tokenId = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId),
            new Claim("display_name", user.DisplayName),
            new Claim("email_verified", user.EmailConfirmed ? "true" : "false")
        };

        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            claims.Add(new Claim("avatar_url", user.AvatarUrl));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new RsaSecurityKey(_privateKey) { KeyId = _jwtOptions.KeyId };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(securityToken);

        return (tokenString, tokenId, expiresAt);
    }

    public (string Token, Guid TokenId, Guid FamilyId) GenerateRefreshToken(string userId, Guid? familyId = null)
    {
        var tokenId = Guid.NewGuid();
        var actualFamilyId = familyId ?? Guid.NewGuid();

        // Create a secure cryptographically random token string
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        var tokenString = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        return (tokenString, tokenId, actualFamilyId);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _jwtOptions.Audience,
            ValidateIssuer = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(_publicKey),
            ValidateLifetime = false // Bypasses expiry validation
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.RsaSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to validate expired token");
            return null;
        }
    }

    private static RSA LoadKey(string base64OrPem)
    {
        var rsa = RSA.Create();
        string pemText;

        if (base64OrPem.Contains("-----BEGIN"))
        {
            pemText = base64OrPem;
        }
        else
        {
            try
            {
                var bytes = Convert.FromBase64String(base64OrPem);
                pemText = Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                pemText = base64OrPem;
            }
        }

        rsa.ImportFromPem(pemText);
        return rsa;
    }

    public RSA GetPublicKey()
    {
        return _publicKey;
    }

    public string GetJwksJson()
    {
        var parameters = _publicKey.ExportParameters(false);
        var modulus = Base64UrlEncoder.Encode(parameters.Modulus);
        var exponent = Base64UrlEncoder.Encode(parameters.Exponent);

        var jwks = new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    alg = "RS256",
                    kid = _jwtOptions.KeyId,
                    n = modulus,
                    e = exponent
                }
            }
        };

        return System.Text.Json.JsonSerializer.Serialize(jwks);
    }
}

using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Auth;

public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtOptions _jwtOptions;
    private readonly IJwtService _jwtService;

    public ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions, IJwtService jwtService)
    {
        _jwtOptions = jwtOptions.Value;
        _jwtService = jwtService;
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(_jwtService.GetPublicKey()),
            ClockSkew = TimeSpan.Zero
        };
    }
}

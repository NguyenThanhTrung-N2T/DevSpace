using Auth.Application.Common.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Persistence;

public class ConfigureIdentityOptions : IConfigureOptions<IdentityOptions>
{
    private readonly SecurityOptions _securityOptions;

    public ConfigureIdentityOptions(IOptions<SecurityOptions> securityOptions)
    {
        _securityOptions = securityOptions.Value;
    }

    public void Configure(IdentityOptions options)
    {
        // Password policies
        options.Password.RequiredLength = _securityOptions.PasswordMinLength;
        options.Password.RequireUppercase = _securityOptions.PasswordRequireUppercase;
        options.Password.RequireLowercase = _securityOptions.PasswordRequireLowercase;
        options.Password.RequireDigit = _securityOptions.PasswordRequireDigit;
        options.Password.RequireNonAlphanumeric = _securityOptions.PasswordRequireNonAlphanumeric;

        // Lockout settings
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;

        // Sign-in settings
        options.SignIn.RequireConfirmedEmail = true;
    }
}

using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Options;
using Auth.Domain.Entities;
using Auth.Infrastructure.Auth;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind Options from Configuration
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Register Data Protection (required for default token providers)
        services.AddDataProtection();

        // Configure Identity Options using Options Pattern
        services.ConfigureOptions<ConfigureIdentityOptions>();

        // Register Authentication & Configure JWT validation options
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        services.ConfigureOptions<ConfigureJwtBearerOptions>();

        // Register EF Core DbContext targeting PostgreSQL
        services.AddDbContext<AuthDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(dbOptions.Default, builder =>
            {
                builder.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName);
                builder.MigrationsHistoryTable("__EFMigrationsHistory", "auth");
            });
        });

        services.AddScoped<IAuthDbContext>(provider => provider.GetRequiredService<AuthDbContext>());

        // Register ASP.NET Identity Core
        services.AddIdentityCore<User>()
            .AddRoles<Role>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        // Register JWT Service (Singleton since keys are loaded/cached in memory)
        services.AddSingleton<IJwtService, JwtService>();

        // Register Specialized Identity & Token Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Register Mock Email Service
        services.AddTransient<IEmailSender, MockEmailSender>();

        return services;
    }
}

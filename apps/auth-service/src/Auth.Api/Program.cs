using Auth.Application;
using Auth.Infrastructure;
using Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting DevSpace Auth API...");

    var builder = WebApplication.CreateBuilder(args);

    // Setup Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Auth.Api")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

    // Register Clean Architecture Layers
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // CORS configuration using Options Pattern
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var corsOrigins = builder.Configuration.GetSection(Auth.Application.Common.Options.CorsOptions.SectionName)
                .Get<Auth.Application.Common.Options.CorsOptions>();
            
            if (corsOrigins?.AllowedOrigins != null && corsOrigins.AllowedOrigins.Length > 0)
            {
                policy.WithOrigins(corsOrigins.AllowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        });
    });

    // Add Controllers & Routing
    builder.Services.AddControllers();

    // Configure Authorization Policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdmin", policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy("RequireVerifiedEmail", policy =>
            policy.RequireClaim("email_verified", "true"));
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "DevSpace Auth API",
            Version = "v1",
            Description = "Developer Workspace Authentication Service"
        });

        // Setup Bearer token security definition
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter JWT access token."
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Configure Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AuthDbContext>("Database_Ready");

    // Register Global Exception Handler
    builder.Services.AddExceptionHandler<Auth.Api.Middleware.GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Register Rate Limiting
    builder.Services.Configure<Auth.Application.Common.Options.RateLimitOptions>(builder.Configuration.GetSection(Auth.Application.Common.Options.RateLimitOptions.SectionName));
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("login", context =>
        {
            var rateLimitOptions = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<Auth.Application.Common.Options.RateLimitOptions>>().Value;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitOptions.LoginPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = rateLimitOptions.LoginQueueLimit
            });
        });

        options.AddPolicy("register", context =>
        {
            var rateLimitOptions = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<Auth.Application.Common.Options.RateLimitOptions>>().Value;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitOptions.RegisterPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = rateLimitOptions.RegisterQueueLimit
            });
        });
    });

    var app = builder.Build();

    // Global Exception Handling (RFC-7807)
    app.UseExceptionHandler();

    // Seed/migrate database automatically on startup
    await AuthDbContextSeeder.SeedAsync(app.Services);

    // Use Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms (TraceId: {TraceId})";
    });

    // HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "openapi/{documentName}.json";
        });

        app.MapScalarApiReference(options =>
        {
            options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
            options.WithTitle("DevSpace Auth API Specification");
            options.WithPreferredScheme("Bearer");
        });
    }

    // Nginx handles HTTPS redirection, disabled within internal containers
    // app.UseHttpsRedirection();

    app.UseRouting();

    app.UseCors();

    app.UseRateLimiter();

    // CORS & Authentication/Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Map JWKS Discovery Endpoint
    app.MapGet("/.well-known/jwks.json", (Auth.Application.Common.Interfaces.IJwtService jwtService) =>
    {
        var jwks = jwtService.GetJwksJson();
        return Results.Content(jwks, "application/json");
    });

    // Map Health Check Endpoints
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false // Live check: returns 200 immediately
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        // Ready check: validates database connection status
    });

    app.Run();
}
catch (Exception ex) when (ex.GetType().Name != "HostAbortedException") // Prevent EF Core migrations run crashes
{
    Log.Fatal(ex, "DevSpace Auth API host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

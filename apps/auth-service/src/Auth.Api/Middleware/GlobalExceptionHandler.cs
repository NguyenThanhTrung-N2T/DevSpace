using Auth.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

        var problemDetails = exception switch
        {
            ValidationException validationException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                Detail = validationException.Message,
                Instance = httpContext.Request.Path
            },
            UnauthorizedException unauthorizedException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Type = "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1",
                Detail = unauthorizedException.Message,
                Instance = httpContext.Request.Path
            },
            NotFoundException notFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                Detail = notFoundException.Message,
                Instance = httpContext.Request.Path
            },
            ConflictException conflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
                Detail = conflictException.Message,
                Instance = httpContext.Request.Path
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server Error",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                Detail = "An unexpected error occurred. Please try again later.",
                Instance = httpContext.Request.Path
            }
        };

        if (exception is ValidationException valEx)
        {
            problemDetails.Extensions["errors"] = valEx.Errors;
        }

        // Populate trace identifier for logs correlation
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

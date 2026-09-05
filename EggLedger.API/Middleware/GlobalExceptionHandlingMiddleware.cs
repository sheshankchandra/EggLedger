using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace EggLedger.API.Middleware;

/// <summary>
/// Last-resort handler for exceptions that escape every controller and service try/catch.
/// Emits the same ProblemDetails envelope as <see cref="Extensions.ResultExtensions"/>.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            OperationCanceledException => (StatusCodes.Status400BadRequest, "Request canceled", "The operation was canceled, likely due to client disconnection or timeout."),
            NpgsqlException => (StatusCodes.Status503ServiceUnavailable, "Database unavailable", "The database service is currently unavailable. Please try again shortly."),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "Request timeout", "The operation took too long to complete."),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "You are not authorized to access this resource."),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "If the problem persists, please contact support."),
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
        };

        var problemDetailsService = context.RequestServices.GetService<IProblemDetailsService>();
        var written = problemDetailsService is not null && await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problemDetails,
            Exception = exception,
        });

        if (!written)
        {
            // Fallback if no writer could handle it (e.g. an unsupported Accept header).
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}

using System.Net;
using System.Text.Json;
using Npgsql;

namespace EggLedger.API.Middleware
{
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

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "An error occurred while processing your request.",
                details = GetErrorDetails(exception),
                timestamp = DateTime.UtcNow,
                statusCode = 500
            };

            switch (exception)
            {
                case OperationCanceledException canceledEx:
                    _logger.LogInformation(canceledEx, "Request was canceled: {Message}", canceledEx.Message);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest; // 400 or could use 499
                    response = new
                    {
                        error = "The request was canceled.",
                        details = "The operation was canceled, likely due to client disconnection or timeout.",
                        timestamp = DateTime.UtcNow,
                        statusCode = 400
                    };
                    break;

                case NpgsqlException npgsqlEx:
                    _logger.LogError(npgsqlEx, "Database connection error: {Message}", npgsqlEx.Message);
                    context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    response = new
                    {
                        error = "Database service is currently unavailable. Please ensure the database server is running and try again.",
                        details = "Please contact your administrator or check if the database service is running.",
                        timestamp = DateTime.UtcNow,
                        statusCode = 503
                    };
                    break;

                case TimeoutException timeoutEx:
                    _logger.LogError(timeoutEx, "Request timeout: {Message}", timeoutEx.Message);
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response = new
                    {
                        error = "The request timed out. Please try again.",
                        details = "The operation took too long to complete.",
                        timestamp = DateTime.UtcNow,
                        statusCode = 408
                    };
                    break;

                case UnauthorizedAccessException unauthorizedEx:
                    _logger.LogWarning(unauthorizedEx, "Unauthorized access attempt: {Message}", unauthorizedEx.Message);
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response = new
                    {
                        error = "You are not authorized to access this resource.",
                        details = "Please log in with appropriate credentials.",
                        timestamp = DateTime.UtcNow,
                        statusCode = 401
                    };
                    break;

                case ArgumentException argEx:
                    _logger.LogWarning(argEx, "Invalid argument provided: {Message}", argEx.Message);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new
                    {
                        error = "Invalid request parameters.",
                        details = argEx.Message,
                        timestamp = DateTime.UtcNow,
                        statusCode = 400
                    };
                    break;

                default:
                    _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response = new
                    {
                        error = "An internal server error occurred. Please try again later.",
                        details = "If the problem persists, please contact support.",
                        timestamp = DateTime.UtcNow,
                        statusCode = 500
                    };
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private string GetErrorDetails(Exception exception)
        {
            return exception switch
            {
                OperationCanceledException => "The request was canceled by the client or due to timeout.",
                NpgsqlException => "Database connection failed. Please ensure PostgreSQL is running.",
                TimeoutException => "Operation timed out. Please try again.",
                UnauthorizedAccessException => "Access denied. Please check your credentials.",
                ArgumentException => "Invalid request parameters provided.",
                _ => "An unexpected error occurred."
            };
        }
    }
}

using System.Text.Json;
using FluentValidation;

namespace ApiRefactor.Infrastructure.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns a consistent, structured JSON error response.
/// Controllers/handlers must NOT contain try/catch around business logic — all exceptions
/// bubble up here.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
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
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failure on {Path}", context.Request.Path);
            await WriteErrorAsync(context, BuildValidationError(ex));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found on {Path}", context.Request.Path);
            await WriteErrorAsync(context, new ErrorResponse(
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                "Not Found",
                StatusCodes.Status404NotFound,
                ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorised access on {Path}", context.Request.Path);
            await WriteErrorAsync(context, new ErrorResponse(
                "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                "Forbidden",
                StatusCodes.Status403Forbidden));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            await WriteErrorAsync(context, new ErrorResponse(
                "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                "Internal Server Error",
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later."));
        }
    }

    private static ErrorResponse BuildValidationError(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ErrorResponse(
            "https://tools.ietf.org/html/rfc4918#section-11.2",
            "Validation Failed",
            StatusCodes.Status422UnprocessableEntity,
            "One or more validation errors occurred.",
            errors);
    }

    private static async Task WriteErrorAsync(HttpContext context, ErrorResponse error)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = error.Status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(error, JsonOptions));
    }
}

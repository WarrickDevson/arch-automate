using System.Text.Json;

namespace ArchAutomate.Api.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteErrorResponse(context, ex);
        }
    }

    private async Task WriteErrorResponse(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        (int statusCode, string message) = ex switch
        {
            ArgumentException or InvalidOperationException => (StatusCodes.Status400BadRequest, ex.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized."),
            KeyNotFoundException => (StatusCodes.Status404NotFound, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = statusCode;

        var body = JsonSerializer.Serialize(new
        {
            status = statusCode,
            error = message,
            detail = env.IsDevelopment() ? ex.ToString() : null,
            traceId = context.TraceIdentifier
        });

        await context.Response.WriteAsync(body);
    }
}

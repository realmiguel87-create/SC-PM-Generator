using System.Net;
using System.Text.Json;
using FluentValidation;

namespace SCPM.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

            var (status, title) = ex switch
            {
                ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
                KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
                UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Access denied"),
                InvalidOperationException => (HttpStatusCode.Conflict, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)status;

            var problem = new
            {
                title,
                status = (int)status,
                errors = ex is ValidationException vex
                    ? vex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
                    : null,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}

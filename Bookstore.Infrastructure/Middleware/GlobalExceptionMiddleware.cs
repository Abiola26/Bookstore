using Bookstore.Application.Common;
using Bookstore.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Bookstore.Infrastructure.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception has occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            NotFoundException ex => (404, ex.Message, new List<string>()),
            ConflictException ex => (409, ex.Message, new List<string>()),
            ValidationException ex => (400, "Validation failed", ex.Errors.ToList()),
            UnauthorizedException ex => (401, ex.Message, new List<string>()),
            ForbiddenException ex => (403, ex.Message, new List<string>()),
            OutOfStockException ex => (400, ex.Message, new List<string>()),
            BusinessException ex => (400, ex.Message, new List<string>()),
            _ => (500, "An internal server error occurred", new List<string>())
        };

        var response = new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors,
            StatusCode = statusCode
        };

        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}

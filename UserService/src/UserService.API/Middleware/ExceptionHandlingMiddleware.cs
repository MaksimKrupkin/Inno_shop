using Microsoft.AspNetCore.Http;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UserService.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Произошла непредвиденная ошибка");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception? ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var errorResponse = new ErrorDetails
        {
            StatusCode = context.Response.StatusCode,
            Message = "Internal Server Error",
            Details = ex?.Message
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}

public class ErrorDetails
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = default!;
    public string? Details { get; set; }
}
using System.Net;
using System.Text.Json;
using BookingMeetingRooms.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BookingMeetingRooms.Api.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var title = "An error occurred while processing your request";
        var detail = exception.Message;

        switch (exception)
        {
            case DomainException domainEx:
                code = HttpStatusCode.BadRequest;
                title = "Domain validation error";
                detail = domainEx.Message;
                break;

            case ArgumentException argEx:
                code = HttpStatusCode.BadRequest;
                title = "Invalid argument";
                detail = argEx.Message;
                break;

            case InvalidOperationException opEx:
                code = HttpStatusCode.BadRequest;
                title = "Invalid operation";
                detail = opEx.Message;
                break;

            case UnauthorizedAccessException:
                code = HttpStatusCode.Forbidden;
                title = "Access denied";
                detail = "You do not have permission to perform this action";
                break;
        }

        var problemDetails = new ProblemDetails
        {
            Status = (int)code,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)code;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}

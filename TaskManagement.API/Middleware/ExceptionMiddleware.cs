using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.API.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation failed: {Message}", ex.Message);
            await WriteResponseAsync(context, StatusCodes.Status400BadRequest, "Validation Error", ex.Message);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning("Not found: {Message}", ex.Message);
            await WriteResponseAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (ForbiddenException ex)
        {
            logger.LogWarning("Forbidden: {Message}", ex.Message);
            await WriteResponseAsync(context, StatusCodes.Status403Forbidden, "Forbidden", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {ExceptionType}", ex.GetType().Name);
            await WriteResponseAsync(context, StatusCodes.Status500InternalServerError,"Internal Server Error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
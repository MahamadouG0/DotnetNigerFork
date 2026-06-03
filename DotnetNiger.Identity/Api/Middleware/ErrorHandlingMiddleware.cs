using System.Net;
using System.Text.Json;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Application.Exceptions;

namespace DotnetNiger.Identity.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, response) = ex switch
        {
            SlugAlreadyExistsException => (HttpStatusCode.Conflict, new ErrorResponse(ex.Message, "SLUG_EXISTS")),
            EmailAlreadyExistsException => (HttpStatusCode.Conflict, new ErrorResponse(ex.Message, "EMAIL_EXISTS")),
            KeyNotFoundException => (HttpStatusCode.NotFound, new ErrorResponse(ex.Message, "NOT_FOUND")),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, new ErrorResponse(ex.Message, "UNAUTHORIZED")),
            InvalidOperationException => (HttpStatusCode.BadRequest, new ErrorResponse(ex.Message, "INVALID_OPERATION")),
            _ => (HttpStatusCode.InternalServerError, new ErrorResponse("Une erreur interne s'est produite", "INTERNAL_ERROR"))
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

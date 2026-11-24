using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WebApi;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
            return false;

        var statusCode = exception switch
        {
            BadHttpRequestException badRequestException => badRequestException.StatusCode,
            InvalidDataException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        if (statusCode is < 400 or > 599)
            statusCode = StatusCodes.Status500InternalServerError;

        if (statusCode >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception processing request");
        else
            logger.LogWarning(exception, "Invalid HTTP request");

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                StatusCodes.Status400BadRequest => "Некорректный запрос",
                StatusCodes.Status413PayloadTooLarge => "Размер запроса превышает допустимый",
                _ => "Внутренняя ошибка сервера"
            },
            Detail = statusCode switch
            {
                StatusCodes.Status400BadRequest => "Проверьте формат тела запроса.",
                StatusCodes.Status413PayloadTooLarge => "Максимальный размер файла — 10 MiB.",
                _ => "При обработке запроса произошла ошибка."
            },
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problem,
            cancellationToken: cancellationToken);
        return true;
    }
}

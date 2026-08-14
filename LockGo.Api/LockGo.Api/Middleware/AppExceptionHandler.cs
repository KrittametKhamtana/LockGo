using LockGo.Api.Contracts;
using LockGo.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace LockGo.Api.Middleware;

/// <summary>
/// Single place that turns any exception into the API's consistent
/// {error:{code,message}} shape. AppException subclasses carry their own
/// status/code; anything else is logged and reported as a bare 500 — no
/// stack trace leaves the process.
/// </summary>
public class AppExceptionHandler : IExceptionHandler
{
    private readonly ILogger<AppExceptionHandler> _logger;

    public AppExceptionHandler(ILogger<AppExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (statusCode, code, message) = exception switch
        {
            AppException appEx => (appEx.StatusCode, appEx.Code, appEx.Message),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred."),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(new ErrorResponse(new ErrorDetail(code, message)), ct);

        return true;
    }
}

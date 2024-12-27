using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security;
using System.Security.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace AITests;

public class GlobalExceptionHandlerMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next.Invoke(context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            context.Response.ContentType = "application/json";
            var code = (int) DetectStatusCode(e);
            context.Response.StatusCode = code >= 500 ? 400 : code;
            await context.Response.WriteAsJsonAsync(e.Message);
        }
    }

    private HttpStatusCode DetectStatusCode(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException or SecurityTokenException or AuthenticationException => HttpStatusCode
                .Unauthorized,

            BadHttpRequestException
                or ArgumentException
                or InvalidOperationException
                or VerificationException
                or ValidationException
                or ArgumentNullException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };
    }
}
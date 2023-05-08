namespace AuthenticationMicroservice.Middleware;

using System.Net;
using Exceptions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

public class ExceptionMiddleware
{
    private readonly ILogger logger;
    private readonly RequestDelegate next;

    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger, RequestDelegate next)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, logger, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, ILogger logger, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
        ResponseEnvelope<object> envelope;

        switch (ex)
        {
            case AuthenticationException authEx:
                var authError = new ErrorDetails
                {
                    Message = authEx.Message,
                    UserMessage = authEx.UserMessage,
                    InnerMessage = authEx.InnerException?.Message
                };

                envelope = new ResponseEnvelope<object>(authError, (int) authEx.HttpStatusCode);
                context.Response.StatusCode = (int) authEx.HttpStatusCode;
                logger.LogWarning(ex.Message);
                break;

            case DbUpdateException dbex:
                envelope = new ResponseEnvelope<object>(dbex,
                    userMessage: "Database Error has occurred. Please try again or report the issue.");
                context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                logger.LogError(ex.Message);
                break;

            default:
                envelope = new ResponseEnvelope<object>(ex);
                context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                logger.LogError(ex.Message);
                break;
        }

        return context.Response.WriteAsync(envelope.ToString());
    }
}

public class ErrorDetails
{
    public string? Message { get; set; }
    public string? UserMessage { get; set; }
    public string? InnerMessage { get; set; }
    public object? Data { get; set; }
}

public class ResponseEnvelope
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }

    public List<ErrorDetails>? Errors { get; set; }
    public object? Data { get; set; } = null;
}

public class ResponseEnvelope<T> : ResponseEnvelope
{
    public ResponseEnvelope()
    {
    }

    public ResponseEnvelope(Exception ex, int statusCode = 400, string? userMessage = null) : this(
        new List<ErrorDetails>
        {
            new()
            {
                Message = ex.Message,
                InnerMessage = ex.InnerException?.Message,
                UserMessage = userMessage
            }
        }, statusCode)
    {
    }

    public ResponseEnvelope(ErrorDetails error, int statusCode)
    {
        Success = false;
        Errors = new List<ErrorDetails> {error};
        StatusCode = statusCode;
    }

    public ResponseEnvelope(List<ErrorDetails> errors, int statusCode)
    {
        Success = false;
        Errors = errors;
        StatusCode = statusCode;
    }

    public ResponseEnvelope(T successData, int statusCode = 200)
    {
        Success = true;
        Data = successData;
        StatusCode = statusCode;
    }

    public new T? Data { get; set; }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}

public class ErrorResponseEnvelope<T> : ResponseEnvelope<T>
{
    public ErrorResponseEnvelope(object error)
    {
    }
}
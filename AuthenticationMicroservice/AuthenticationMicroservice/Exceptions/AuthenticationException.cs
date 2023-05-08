namespace AuthenticationMicroservice.Exceptions;

using System.Net;

public class AuthenticationException : Exception
{
    private readonly string userMessage;

    public AuthenticationException()
    {
    }

    public AuthenticationException(string message, HttpStatusCode code = HttpStatusCode.BadRequest,
        Exception? innerException = null)
        : this(message, innerException)
    {
        HttpStatusCode = code;
    }

    public AuthenticationException(string message) : base(message)
    {
    }

    public AuthenticationException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    public string UserMessage => string.IsNullOrEmpty(userMessage) ? Message : userMessage;

    public HttpStatusCode HttpStatusCode { get; internal set; } = HttpStatusCode.BadRequest;
}
namespace Group17profile.Exceptions;

using System.Net;

public class ProfileException : Exception
{
    private readonly string userMessage;

    public ProfileException()
    {
    }

    public ProfileException(string message, HttpStatusCode code = HttpStatusCode.BadRequest,
        Exception? innerException = null)
        : this(message, innerException)
    {
        HttpStatusCode = code;
    }

    public ProfileException(string message) : base(message)
    {
    }

    public ProfileException(string message, Exception? innerException) : base(message, innerException)
    {
    }

    public string UserMessage => string.IsNullOrEmpty(userMessage) ? Message : userMessage;

    public HttpStatusCode HttpStatusCode { get; internal set; } = HttpStatusCode.BadRequest;
}
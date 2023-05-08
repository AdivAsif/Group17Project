namespace Group17PortalWasm.API.Exception;

using Exception = System.Exception;

public class APIException : Exception
{
    public APIException(string message, int statusCode, string response,
        IReadOnlyDictionary<string, IEnumerable<string>> headers, Exception innerException) : base(
        message + "\n\nStatus: " + statusCode + "\nResponse: \n" + response, innerException)
    {
        StatusCode = statusCode;
        Response = response;
        Headers = headers;
    }

    public int StatusCode { get; }

    public string Response { get; private set; }

    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    public override string ToString()
    {
        if (!Response.Contains("System.Exception"))
            return Response;
        Response = Response.Replace("System.Exception: ", "");
        var index = Response.IndexOf("\r\n", StringComparison.Ordinal);
        if (index > 0)
            Response = Response[..index];
        return Response;
    }
}
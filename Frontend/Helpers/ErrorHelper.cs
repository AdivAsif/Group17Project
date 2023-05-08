namespace Group17PortalWasm.Helpers;

using System.Text;
using Group17.Auth;
using Newtonsoft.Json;

public static class ErrorHelper
{
    public static ApiErrorMessage UnwrapError(Exception exception)
    {
        ApiErrorMessage unwrappedError = null;
        if (exception is APIException apiException)
        {
            if (!string.IsNullOrWhiteSpace(apiException.Response))
            {
                try
                {
                    unwrappedError = JsonConvert.DeserializeObject<ApiErrorMessage>(apiException.Response);
                }
                catch
                {
                    var apiError = JsonConvert.DeserializeObject<ApiError>(apiException.Response);
                    unwrappedError = new ApiErrorMessage
                    {
                        Success = false,
                        StatusCode = apiError.Status,
                        Errors = new List<ErrorDetails>()
                    };

                    foreach (var error in apiError.Errors)
                        unwrappedError.Errors.Add(new ErrorDetails {Message = string.Join(", ", error.Value)});
                }
            }
            else
            {
                unwrappedError = new ApiErrorMessage
                {
                    Success = false,
                    StatusCode = apiException.StatusCode,
                    Errors = new List<ErrorDetails>()
                };
                try
                {
                    foreach (var error in
                             ((APIException<BadRequestObjectResultResponseEnvelope>) apiException).Result
                             .Errors)
                        unwrappedError.Errors.Add(new ErrorDetails {Message = string.Join(", ", error.Data)});
                }
                catch
                {
                    // ignored
                }
            }
        }

        return unwrappedError ??= new ApiErrorMessage
        {
            Success = false,
            Errors = new List<ErrorDetails>
            {
                new() {Message = exception.Message}
            }
        };
    }
}

public class ApiErrorMessage
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public List<ErrorDetails> Errors { get; set; } = new();
    public object? Data { get; set; } = null;

    public override string ToString()
    {
        StringBuilder builder = new();
        foreach (var apiError in Errors)
        {
            if (!string.IsNullOrEmpty(apiError.Message))
                builder.AppendLine(apiError.Message);

            if (!string.IsNullOrEmpty(apiError.InnerMessage))
                builder.AppendLine(apiError.InnerMessage);

            if (apiError.Data is not string s) continue;
            if (!string.IsNullOrEmpty(s)) builder.AppendLine(s);
        }

        return builder.ToString();
    }
}

public class ErrorDetails
{
    public string? Message { get; set; }
    public string? UserMessage { get; set; }
    public string? InnerMessage { get; set; }
    public object? Data { get; set; }
}

public class ApiError
{
    public string? Title { get; set; }
    public int Status { get; set; }
    public string? TraceId { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}
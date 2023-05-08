namespace Group17profile.Middleware;

using System.Net;
using System.Text;

public class SwaggerAuthMiddleware
{
    private readonly RequestDelegate _next;

    public SwaggerAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger") && !IsLocalRequest(context))
        {
            string authHeader = context.Request.Headers["Authorization"]!;
            if (authHeader.StartsWith("Basic "))
            {
                var encodedUsernamePassword =
                    authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1].Trim();

                if (string.IsNullOrEmpty(encodedUsernamePassword))
                {
                    context.Response.Headers["WWW-Authenticate"] = "Basic";
                    context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    return;
                }

                var decodedUsernamePassword =
                    Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));

                var username = decodedUsernamePassword.Split(':', 2)[0];
                var password = decodedUsernamePassword.Split(':', 2)[1];

                if (IsAuthorized(username, password))
                {
                    await _next.Invoke(context);
                    return;
                }
            }

            context.Response.Headers["WWW-Authenticate"] = "Basic";

            context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
        }
        else
        {
            await _next.Invoke(context);
        }
    }

    public static bool IsAuthorized(string username, string password)
    {
        return username.Equals("Auth", StringComparison.InvariantCultureIgnoreCase) &&
               password.Equals("Documentation");
    }

    public static bool IsLocalRequest(HttpContext context)
    {
        switch (context.Connection.RemoteIpAddress)
        {
            case null when context.Connection.LocalIpAddress == null:
            case null:
                return true;
        }

        return context.Connection.RemoteIpAddress.Equals(context.Connection.LocalIpAddress);
    }
}
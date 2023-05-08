namespace Group17PortalWasm.Helpers;

using Group17.Auth;
using Polly;

public static class PollyExtensions
{
    private static readonly string ClientKey = "LoggerKey";
    private static readonly string RefreshTokenKey = "RefreshTokenKey";

    public static Context WithRefreshToken(this Context context, string refreshToken)
    {
        context[RefreshTokenKey] = refreshToken;
        return context;
    }

    public static Context WithClient(this Context context, IAuthClient client)
    {
        context[ClientKey] = client;
        return context;
    }

    public static IAuthClient? GetAuthClient(this Context context)
    {
        if (context.TryGetValue(ClientKey, out var authClient))
            return authClient as IAuthClient;
        return null;
    }

    public static string? GetRefreshToken(this Context context)
    {
        return context.TryGetValue(RefreshTokenKey, out var refreshToken) ? refreshToken?.ToString() : null;
    }
}
namespace Group17profile.Middleware;

public static class SwaggerAuthExtensions
{
    public static IApplicationBuilder UseSwaggerAuthorized(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SwaggerAuthMiddleware>();
    }
}
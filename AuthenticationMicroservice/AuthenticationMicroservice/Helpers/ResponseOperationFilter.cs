namespace AuthenticationMicroservice.Helpers;

using System.Reflection;
using Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class ResponseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .Union(context.MethodInfo.ReflectedType?.GetCustomAttributes(true) ?? Array.Empty<object>())
            .OfType<AuthorizeAttribute>();

        var anonAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .Union(context.MethodInfo.ReflectedType?.GetCustomAttributes(true) ?? Array.Empty<object>())
            .OfType<AllowAnonymousAttribute>();

        var authorizeAttributes = authAttributes as AuthorizeAttribute[] ?? authAttributes.ToArray();
        if (authAttributes != null && authorizeAttributes.Any() && !operation.Responses.TryGetValue("401", out _))
            operation.Responses.Add("401", new OpenApiResponse {Description = "Unauthorized"});

        if (authAttributes != null && authorizeAttributes.Any() && !operation.Responses.TryGetValue("403", out _))
            operation.Responses.Add("403", new OpenApiResponse {Description = "Forbidden"});

        if (authAttributes != null && authorizeAttributes.Any() && anonAttributes != null && !anonAttributes.Any())
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Id = "Bearer",
                            Type = ReferenceType.SecurityScheme
                        }
                    },
                    new List<string>()
                }
            });

        var apiTokenAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .Union(context.MethodInfo.ReflectedType.GetCustomAttributes(true))
            .OfType<AuthorizeRoles>();

        if (apiTokenAttributes != null && apiTokenAttributes.Any() && anonAttributes != null && !anonAttributes.Any())
        {
            var roleField = apiTokenAttributes.First().GetType()
                .GetField("roles", BindingFlags.NonPublic | BindingFlags.Instance);

            if (roleField == null)
                return;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            var roles = (Constants.Roles[]) roleField.GetValue(apiTokenAttributes.First());
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (roles != null && roles.Contains(Constants.Roles.Api))
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "Authentication-API-Key",
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        new List<string>()
                    }
                });

            if (roles != null) operation.Description += "Required Roles: " + string.Join(", ", roles);
        }
    }
}
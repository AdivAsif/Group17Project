namespace Group17profile.Controllers;

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeRoles : AuthorizeAttribute, IAuthorizationFilter
{
    private readonly Constants.Roles[] _roles;

    public AuthorizeRoles(params Constants.Roles[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity is {IsAuthenticated: false})
            return;


        if (_roles.Any(role => user.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role.ToString())))
            return;

        context.Result = new StatusCodeResult((int) HttpStatusCode.Forbidden);
    }
}
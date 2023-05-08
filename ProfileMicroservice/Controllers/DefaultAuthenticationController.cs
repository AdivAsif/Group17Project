namespace Group17profile.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class DefaultAuthenticationController : DefaultController
{
    protected int UserId
    {
        get
        {
            if (HasUserId)
                if (int.TryParse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value,
                        out var userIdFromClaim))
                    return userIdFromClaim;

            if (!IsApi && !IsServer) throw new UnauthorizedAccessException("User ID not present");
            if (!Request.Headers.TryGetValue(Constants.HeaderKeys.UserId, out var userIdHeaderString))
                throw new UnauthorizedAccessException("User ID not sent with server request");
            if (int.TryParse(userIdHeaderString, out var userId))
                return userId;

            throw new UnauthorizedAccessException("User ID not sent with server request");
        }
    }

    private bool HasUserId => User.HasClaim(c => c.Type == ClaimTypes.Sid);
    private bool IsApi => User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == Constants.Roles.Api.ToString());

    private bool IsServer =>
        User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == Constants.Roles.Server.ToString());
}
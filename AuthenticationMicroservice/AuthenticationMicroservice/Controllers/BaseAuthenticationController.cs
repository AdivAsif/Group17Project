namespace AuthenticationMicroservice.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class BaseAuthController : BaseController
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

    protected string? GetAccessToken
    {
        get { return User.Claims.FirstOrDefault(c => c.Type == Constants.Tokens.AccessToken)?.Value; }
    }

    protected string? GetRefreshToken
    {
        get { return User.Claims.FirstOrDefault(c => c.Type == Constants.Tokens.RefreshToken)?.Value; }
    }

    protected string? GetProfileSasToken
    {
        get { return User.Claims.FirstOrDefault(c => c.Type == Constants.Tokens.ProfileSasToken)?.Value; }
    }
}
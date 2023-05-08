namespace Group17PortalWasm.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Helpers;
using Microsoft.AspNetCore.Components.Authorization;

public class UserInfo
{
    public UserInfo(AuthenticationState authState)
    {
        var jwtSecurityToken = ReadToken(authState);

        UserId = int.Parse(ClaimsHelper.GetStringClaim(jwtSecurityToken, ClaimTypes.Sid));
        EmailAddress = ClaimsHelper.GetStringClaim(jwtSecurityToken, "email");
        SignUpDate = ClaimsHelper.GetStringClaim(jwtSecurityToken, Constants.Claims.UserSignUpDate);
        Firstname = ClaimsHelper.GetStringClaim(jwtSecurityToken, Constants.Claims.Firstname);
        Surname = ClaimsHelper.GetStringClaim(jwtSecurityToken, Constants.Claims.Surname);
        Username = ClaimsHelper.GetStringClaim(jwtSecurityToken, Constants.Claims.Username);

        if (jwtSecurityToken != null)
            Roles = jwtSecurityToken.Claims.Where(c => c.Type.ToLower().Equals("role")).Select(c => c.Value).ToArray();
    }

    public int UserId { get; init; }
    public string EmailAddress { get; init; }
    public string Firstname { get; init; }
    public string Surname { get; init; }
    public string Username { get; init; }
    public string SignUpDate { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();

    public bool IsAdmin => Roles.Contains(Constants.Roles.Administrator.ToString());

    private static JwtSecurityToken? ReadToken(AuthenticationState authState)
    {
        var identity = (ClaimsIdentity) authState.User.Identity!;

        var handler = new JwtSecurityTokenHandler();
        var accessToken = identity.Claims.FirstOrDefault(x => x.Type == Constants.Claims.AccessToken)?.Value;

        return accessToken == null ? null : handler.ReadJwtToken(accessToken);
    }
}
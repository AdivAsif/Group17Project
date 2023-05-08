namespace Group17PortalWasm.Helpers;

using System.IdentityModel.Tokens.Jwt;
using Group17.Auth;
using Microsoft.AspNetCore.Components.Authorization;

public static class ClaimsHelper
{
    public static string GetStringClaim(JwtSecurityToken? jwtToken, string claim)
    {
        if (jwtToken == null)
            return string.Empty;

        var claimText = jwtToken.Claims.FirstOrDefault(x => x.Type == claim)?.Value;

        return claimText ?? string.Empty;
    }

    public static bool GetBoolClaim(JwtSecurityToken? jwtToken, string claim)
    {
        if (jwtToken == null)
            return false;

        var claimText = jwtToken.Claims.FirstOrDefault(x => x.Type == claim)?.Value;

        if (claimText == null)
            return false;

        return bool.Parse(claimText);
    }

    public static int[] GetIntArrayClaim(JwtSecurityToken? jwtToken, string claim)
    {
        if (jwtToken == null)
            return Array.Empty<int>();

        var claimText = jwtToken.Claims.FirstOrDefault(x => x.Type == claim)?.Value;

        if (claimText != null) return Array.ConvertAll(claimText.Split(","), int.Parse);

        return Array.Empty<int>();
    }

    public static string? GetRefreshToken(AuthenticationState authState)
    {
        if (authState.User.Identity is null)
            return string.Empty;

        var identity = (ClaimsIdentity) authState.User.Identity;

        if (identity == null)
            return string.Empty;

        var handler = new JwtSecurityTokenHandler();
        var accessToken = identity.Claims.FirstOrDefault(x => x.Type == Constants.Claims.RefreshToken)?.Value;

        return accessToken;
    }


    public static async Task RefreshToken(IAuthClient authClient,
        AuthenticationStateProvider authenticationStateProvider)
    {
        var authenticationState = await ((CustomAuthenticationStateProvider) authenticationStateProvider)
            .GetAuthenticationStateAsync();

        var user = authenticationState.User;

        var refreshToken = user.Claims.FirstOrDefault(c => c.Type == Constants.Claims.RefreshToken)?.Value;

        if (authClient != null && !string.IsNullOrEmpty(refreshToken))
            try
            {
                var tokenResponse = await authClient.RefreshTokenAsync(new RefreshTokenRequestDTO
                    {RefreshToken = refreshToken});

                if (tokenResponse.Success)
                    await ((CustomAuthenticationStateProvider) authenticationStateProvider).SetAuthenticationStateAsync(
                        tokenResponse.Data);
            }
            catch (APIException ex)
            {
                await ((CustomAuthenticationStateProvider) authenticationStateProvider).ClearAuthenticationStateAsync();
                throw new Exception(ex.Message);
            }
    }
}
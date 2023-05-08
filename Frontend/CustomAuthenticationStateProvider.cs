namespace Group17PortalWasm;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Group17.Auth;
using Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using Claim = System.Security.Claims.Claim;
using ClaimsIdentity = System.Security.Claims.ClaimsIdentity;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorageService;

    public CustomAuthenticationStateProvider(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var accessToken = await _localStorageService.GetItemAsStringAsync(Constants.Claims.AccessToken);
        var refreshToken = await _localStorageService.GetItemAsStringAsync(Constants.Claims.RefreshToken);
        ClaimsIdentity identity;
        if (!string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(refreshToken))
            identity = GetClaimsFromAccessToken(accessToken, refreshToken);
        else
            identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    public async Task SetAuthenticationStateAsync(AuthenticatedUserDTO authenticatedUserDto)
    {
        await _localStorageService.SetItemAsStringAsync("accessToken", authenticatedUserDto.AccessToken);
        await _localStorageService.SetItemAsStringAsync("refreshToken", authenticatedUserDto.RefreshToken);
        var identity = GetClaimsFromAccessToken(authenticatedUserDto.AccessToken, authenticatedUserDto.RefreshToken);
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task ClearAuthenticationStateAsync()
    {
        await _localStorageService.ClearAsync();
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    private static ClaimsIdentity GetClaimsFromAccessToken(string accessToken, string refreshToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(accessToken);
        var claims = jwtToken.Claims.ToList();
        var roles = jwtToken.Claims.Where(c => c.Type == "role");
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.Value)));
        claims.Add(new Claim(Constants.Claims.AccessToken, accessToken));
        claims.Add(new Claim(Constants.Claims.RefreshToken, refreshToken));
        return new ClaimsIdentity(claims, "Token");
    }
}
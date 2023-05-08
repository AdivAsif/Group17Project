namespace Group17PortalWasm.API.Base;

using System.Net;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Group17.Auth;
using Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using Polly;
using Polly.Retry;
using ApiException = Exception.APIException;
using Exception = System.Exception;

public class Group17APIHttpMessageHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    private readonly IHttpClientFactory _httpClientFactory;
    private AsyncRetryPolicy? _retryPolicy;
    private AsyncRetryPolicy<HttpResponseMessage>? _unauthorizedPolicy;

    public Group17APIHttpMessageHandler(IHttpClientFactory httpClientFactory, ILocalStorageService localStorageService,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _httpClientFactory = httpClientFactory;
        _authenticationStateProvider = authenticationStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_authenticationStateProvider == null)
            throw new Exception("AuthenticationStateProvider is null.");

        try
        {
            var authenticationState = await ((CustomAuthenticationStateProvider) _authenticationStateProvider)
                .GetAuthenticationStateAsync();
            var user = authenticationState.User;
            var bearerToken = user.Claims.FirstOrDefault(c => c.Type == Constants.Claims.AccessToken)?.Value;
            var refreshToken = user.Claims.FirstOrDefault(c => c.Type == Constants.Claims.RefreshToken)?.Value;

            var context = new Context();
            if (!string.IsNullOrWhiteSpace(refreshToken))
                context.WithRefreshToken(refreshToken);

            var httpClient = _httpClientFactory.CreateClient(Constants.HttpClientNames.ApiClient);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            context.WithClient(new AuthClient(httpClient));

            if (!string.IsNullOrWhiteSpace(bearerToken))
                request.Headers.Add("Authorization", $"Bearer {bearerToken}");

            _unauthorizedPolicy ??= Policy
                .HandleResult<HttpResponseMessage>(ex => ex.StatusCode == HttpStatusCode.Unauthorized)
                .RetryAsync(1, async (_, __, context) =>
                {
                    var newAccessToken = await AttemptRefreshTokenAsync(context);
                    context["AuthToken"] = newAccessToken;
                });

            _retryPolicy ??= Policy.Handle<HttpRequestException>().RetryAsync(1);

            var result = await _unauthorizedPolicy.WrapAsync(_retryPolicy).ExecuteAsync(async context =>
            {
                if (!context.TryGetValue("AuthToken", out var value))
                    return await base.SendAsync(request, cancellationToken);
                var newAccessToken = Convert.ToString(value);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);

                return await base.SendAsync(request, cancellationToken);
            }, contextData: context);

            if (result.StatusCode == HttpStatusCode.Forbidden)
                throw new Exception("Forbidden.");
            return result;
        }
        catch (ApiException ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private async Task<string> AttemptRefreshTokenAsync(Context context)
    {
        var authClient = context.GetAuthClient();
        var refreshToken = context.GetRefreshToken();

        if (authClient == null || string.IsNullOrEmpty(refreshToken))
            throw new Exception("Cannot retrieve refresh token.");
        try
        {
            var tokenResponse =
                await authClient.RefreshTokenAsync(new RefreshTokenRequestDTO {RefreshToken = refreshToken});

            if (tokenResponse.Success)
            {
                await ((CustomAuthenticationStateProvider) _authenticationStateProvider).SetAuthenticationStateAsync(
                    tokenResponse.Data);
                return tokenResponse.Data.AccessToken;
            }
        }
        catch (APIException ex)
        {
            await ((CustomAuthenticationStateProvider) _authenticationStateProvider).ClearAuthenticationStateAsync();
            throw new Exception(ex.Message);
        }

        throw new Exception("Cannot retrieve refresh token.");
    }
}
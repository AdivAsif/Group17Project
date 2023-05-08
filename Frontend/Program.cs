using Blazored.LocalStorage;
using Group17PortalWasm;
using Group17PortalWasm.API.Base;
using Group17PortalWasm.Helpers;
using Group17PortalWasm.Services;
using Group17PortalWasm.Settings;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient {BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)});
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddSingleton<ProfileStateService>();
builder.Services.AddScoped<AppState>();
builder.Services.AddTransient<Group17APIHttpMessageHandler>();
var appSettingsSection = builder.Configuration.GetSection("AppSettings");
var appSettings = appSettingsSection.Get<AppSettings>();

builder.Services.AddClientsForApiAuth(appSettings.AuthMicroserviceBaseUrl);
builder.Services.AddClientsForApiProfile(appSettings.ProfileMicroserviceBaseUrl);
builder.Services.AddClientsForApiTVSeries(appSettings.TVSeriesMicroserviceBaseUrl);
builder.Services.AddClientsForApiReviewsRatings(appSettings.ReviewsRatingsMicroserviceBaseUrl);

builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddAuthenticationCore();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddSingleton<HubConnection>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
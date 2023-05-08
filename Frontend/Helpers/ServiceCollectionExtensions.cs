namespace Group17PortalWasm.Helpers;

using System.Reflection;
using API;
using API.Base;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGeneratedApiClients(this IServiceCollection services, string baseUrl)
    {
        services.AddTransient<Group17APIHttpMessageHandler>();

        var thisAssembly = Assembly.GetExecutingAssembly();

        var apiClients = thisAssembly.GetTypes().Where(t => !t.IsInterface && typeof(IAPIBase).IsAssignableFrom(t));

        foreach (var client in apiClients)
        {
            var clientInterface = client.GetInterfaces().FirstOrDefault(i => i.Name != nameof(IAPIBase));
            var methodInfo = GetAddHttpClientGenericMethod();

            if (clientInterface != null && methodInfo != null)
                services.AddTypedHttpService(clientInterface, client, methodInfo, baseUrl);
        }

        return services;
    }

    public static IServiceCollection AddHttpClientForApiAuth(this IServiceCollection services, string baseUrl)
    {
        services.AddHttpClient(Constants.HttpClientNames.ApiClient, GetHttpClientConfig(baseUrl));
        return services;
    }

    public static IServiceCollection AddClientsForApiAuth(this IServiceCollection services, string baseUrl)
    {
        var thisAssembly = Assembly.GetExecutingAssembly();
        var authClients = thisAssembly.GetTypes().Where(t => !t.IsInterface && typeof(IAuthBase).IsAssignableFrom(t));

        foreach (var client in authClients)
        {
            var clientInterface = client.GetInterfaces().FirstOrDefault(i => i.Name != nameof(IAuthBase));
            var methodInfo = GetAddHttpClientGenericMethod();

            if (clientInterface != null && methodInfo != null)
                services.AddTypedHttpService(clientInterface, client, methodInfo, baseUrl);
        }

        return services;
    }

    public static IServiceCollection AddClientsForApiProfile(this IServiceCollection services, string baseUrl)
    {
        var thisAssembly = Assembly.GetExecutingAssembly();
        var profileClients = thisAssembly.GetTypes()
            .Where(t => !t.IsInterface && typeof(IProfileBase).IsAssignableFrom(t));

        foreach (var client in profileClients)
        {
            var clientInterface = client.GetInterfaces().FirstOrDefault(i => i.Name != nameof(IProfileBase));
            var methodInfo = GetAddHttpClientGenericMethod();

            if (clientInterface != null && methodInfo != null)
                services.AddTypedHttpService(clientInterface, client, methodInfo, baseUrl);
        }

        return services;
    }

    public static IServiceCollection AddClientsForApiTVSeries(this IServiceCollection services, string baseUrl)
    {
        var thisAssembly = Assembly.GetExecutingAssembly();
        var tvClients = thisAssembly.GetTypes()
            .Where(t => !t.IsInterface && typeof(ITVBase).IsAssignableFrom(t));

        foreach (var client in tvClients)
        {
            var clientInterface = client.GetInterfaces().FirstOrDefault(i => i.Name != nameof(ITVBase));
            var methodInfo = GetAddHttpClientGenericMethod();

            if (clientInterface != null && methodInfo != null)
                services.AddTypedHttpService(clientInterface, client, methodInfo, baseUrl);
        }

        return services;
    }

    public static IServiceCollection AddClientsForApiReviewsRatings(this IServiceCollection services, string baseUrl)
    {
        var thisAssembly = Assembly.GetExecutingAssembly();
        var reviewsRatingsClients = thisAssembly.GetTypes()
            .Where(t => !t.IsInterface && typeof(IReviewsRatingsBase).IsAssignableFrom(t));

        foreach (var client in reviewsRatingsClients)
        {
            var clientInterface = client.GetInterfaces().FirstOrDefault(i => i.Name != nameof(IReviewsRatingsBase));
            var methodInfo = GetAddHttpClientGenericMethod();

            if (clientInterface != null && methodInfo != null)
                services.AddTypedHttpService(clientInterface, client, methodInfo, baseUrl);
        }

        return services;
    }


    private static IServiceCollection AddTypedHttpService(this IServiceCollection services, Type clientInterface,
        Type clientImplementation, MethodInfo addHttpClientMethodInfo, string baseUrl)
    {
        var addHttpClientMethod = addHttpClientMethodInfo.MakeGenericMethod(clientInterface, clientImplementation);
        var t = addHttpClientMethod.Invoke(null, new object[] {services, GetHttpClientConfig(baseUrl)});
        (t as IHttpClientBuilder).AddHttpMessageHandler<Group17APIHttpMessageHandler>();

        return services;
    }

    private static Action<HttpClient> GetHttpClientConfig(string baseUrl)
    {
        return h => { h.BaseAddress = new Uri(baseUrl); };
    }

    private static MethodInfo? GetAddHttpClientGenericMethod()
    {
        // IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services, Action<IServiceProvider, HttpClient> configureClient)
        // can also use the variant with Action<IServiceProvider, HttpClient> if want to use services in the config

        return typeof(HttpClientFactoryServiceCollectionExtensions).GetMethods()
            .FirstOrDefault(m => m.Name == "AddHttpClient" &&
                                 m.IsGenericMethod &&
                                 m.GetParameters().Length == 2 &&
                                 m.GetParameters()[0].ParameterType == typeof(IServiceCollection) &&
                                 m.GetParameters()[1].ParameterType ==
                                 typeof(Action<HttpClient>) && // m.GetParameters()[1].ParameterType == typeof(Action<IServiceProvider, HttpClient>) &&
                                 m.GetGenericArguments().Length == 2);
    }
}
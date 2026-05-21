using System.Diagnostics.CodeAnalysis;

namespace EprRegulatorGateway.Utils.Http;

[ExcludeFromCodeCoverage]
public static class HttpClientRegistrationExtension
{
    public static IHttpClientBuilder AddHttpClientWithTracing<TClient, TImplementation>(
        this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddTransient<ProxyHttpMessageHandler>();

        return services
            .AddHttpClient<TClient, TImplementation>()
            .AddHeaderPropagation();
    }

    public static IHttpClientBuilder AddHttpClientWithTracing<TClient, TImplementation>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient> configureClient)
        where TClient : class
        where TImplementation : class, TClient
    {
        return services
            .AddHttpClient<TClient, TImplementation>(configureClient)
            .AddHeaderPropagation();
    }

    public static IHttpClientBuilder AddHttpClientWithTracingAndProxy<TClient, TImplementation>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient> configureClient)
        where TClient : class
        where TImplementation : class, TClient
    {
        Console.WriteLine($"AddHttpClientWithTracingAndProxy<{typeof(TImplementation).Name}>: HTTP_PROXY={Environment.GetEnvironmentVariable("HTTP_PROXY")}, HTTPS_PROXY={Environment.GetEnvironmentVariable("HTTPS_PROXY")}, NO_PROXY={Environment.GetEnvironmentVariable("NO_PROXY")}");

        services.AddTransient<ProxyHttpMessageHandler>();

        return services
            .AddHttpClient<TClient, TImplementation>(configureClient)
            .AddHeaderPropagation()
            .ConfigurePrimaryHttpMessageHandler<ProxyHttpMessageHandler>();
    }
}
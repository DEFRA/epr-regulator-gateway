using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace EprRegulatorGateway.Utils.Http;

[ExcludeFromCodeCoverage]
public class ProxyHttpMessageHandler : HttpClientHandler
{
    private readonly ILogger<ProxyHttpMessageHandler> _logger;

    [ExcludeFromCodeCoverage]
    public ProxyHttpMessageHandler(ILogger<ProxyHttpMessageHandler> logger)
    {
        _logger = logger;
        var proxyUri = Environment.GetEnvironmentVariable("HTTP_PROXY");
        var proxy = new WebProxy { BypassProxyOnLocal = true };
        if (proxyUri != null)
        {
            logger.LogDebug("Creating proxy http client");
            var uri = new UriBuilder(proxyUri).Uri;
            proxy.Address = uri;
        }
        else
        {
            logger.LogWarning("HTTP_PROXY is NOT set, proxy client will be disabled");
        }

        Proxy = proxy;
        UseProxy = proxyUri != null;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ProxyHttpMessageHandler sending. RequestUri={RequestUri}, UseProxy={UseProxy}, HandlerProxyAddress={HandlerProxyAddress}, ResolvedProxyForRequest={ResolvedProxyForRequest}, HttpProxyEnv={HttpProxyEnv}",
            request.RequestUri,
            UseProxy,
            (Proxy as WebProxy)?.Address,
            UseProxy && Proxy is not null && request.RequestUri is not null ? Proxy.GetProxy(request.RequestUri) : null,
            Environment.GetEnvironmentVariable("HTTP_PROXY"));

        return base.SendAsync(request, cancellationToken);
    }
}
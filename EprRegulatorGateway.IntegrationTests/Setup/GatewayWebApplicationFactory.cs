using System.Security.Claims;
using EprRegulatorGateway.Account.Services;
using EprRegulatorGateway.Authentication;
using EprRegulatorGateway.MockBackendAccount;
using EprRegulatorGateway.MockTokenServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WireMock.Server;

namespace EprRegulatorGateway.IntegrationTests.Setup;

public sealed class GatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly List<Action<IWebHostBuilder>> _configure = [];
    private readonly Lazy<WireMockServer> _backendAccountServer =
        new(() => MockBackendAccountServer.Start(useSsl: false));
    private readonly Lazy<WireMockServer> _tokenServer =
        new(() => MockTokenServerHost.Start(useSsl: false));

    private bool _useFakeAccountService = true;

    public WireMockServer BackendAccountServer => _backendAccountServer.Value;

    public WireMockServer TokenServer => _tokenServer.Value;

    public GatewayWebApplicationFactory WithWebHostConfiguration(Action<IWebHostBuilder> configure)
    {
        _configure.Add(configure);
        return this;
    }

    public GatewayWebApplicationFactory UseFakeAccountService(bool useFakeAccountService)
    {
        _useFakeAccountService = useFakeAccountService;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Acl:Clients:IntegrationTest:Type", nameof(AclOptions.ClientType.OAuth));
        builder.UseSetting("Acl:Clients:IntegrationTest:Scopes:0", Scopes.Read);

        if (_useFakeAccountService)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAccountService>();
                services.AddScoped<IAccountService, FakeAccountService>();
            });
        }
        else
        {
            builder.UseSetting("BackendAccountService:BaseUrl", BackendAccountServer.Url);
            builder.UseSetting("BackendAccountService:Scope", "");
        }

        foreach (var configure in _configure)
        {
            configure(builder);
        }
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            JwtAuthenticationHandler.SchemeName,
            TestJwt.GenerateJwt(new Claim(Claims.ClientId, "IntegrationTest")));
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_backendAccountServer.IsValueCreated)
            {
                _backendAccountServer.Value.Stop();
                _backendAccountServer.Value.Dispose();
            }

            if (_tokenServer.IsValueCreated)
            {
                _tokenServer.Value.Stop();
                _tokenServer.Value.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}

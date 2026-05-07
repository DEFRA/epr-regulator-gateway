using System.Security.Claims;
using EprRegulatorGateway.Account.Services;
using EprRegulatorGateway.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EprRegulatorGateway.IntegrationTests.Setup;

public sealed class GatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly List<Action<IWebHostBuilder>> _configure = [];
    private bool _useFakeAccountService = true;

    public GatewayWebApplicationFactory()
    {
    }

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
}

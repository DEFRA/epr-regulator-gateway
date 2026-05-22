using WireMock.Server;
using Xunit;

namespace EprRegulatorGateway.IntegrationTests.Setup;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected GatewayWebApplicationFactory Factory { get; private set; } = null!;

    protected WireMockServer BackendAccountServer => Factory.BackendAccountServer;

    protected WireMockServer TokenServer => Factory.TokenServer;

    protected virtual void ConfigureFactory(GatewayWebApplicationFactory factory)
    {
        factory.UseFakeAccountService(false);
    }

    public virtual ValueTask InitializeAsync()
    {
        Factory = new GatewayWebApplicationFactory();
        ConfigureFactory(Factory);

        // Avoid mystery-guest from default canned mappings. Tests must define their own stubs explicitly.
        BackendAccountServer.ResetMappings();
        TokenServer.ResetMappings();

        return ValueTask.CompletedTask;
    }

    public virtual async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}

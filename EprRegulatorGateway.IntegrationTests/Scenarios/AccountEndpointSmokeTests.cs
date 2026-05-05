using System.Net;
using EprRegulatorGateway.IntegrationTests.Setup;
using Xunit;

namespace EprRegulatorGateway.IntegrationTests.Scenarios;

[Trait("Category", "IntegrationTests")]
public sealed class AccountEndpointSmokeTests : IClassFixture<GatewayWebApplicationFactory>
{
    private readonly GatewayWebApplicationFactory _factory;

    public AccountEndpointSmokeTests(GatewayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static Uri HealthUri => new("/health", UriKind.Relative);
    private static Uri AccountUri(Guid userId) => new($"/api/account/{userId:D}", UriKind.Relative);

    [Fact]
    public async Task Health_returns_ok_without_authorization()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(HealthUri, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Account_without_authorization_returns_unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            AccountUri(Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Account_with_valid_jwt_returns_ok()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync(
            AccountUri(Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

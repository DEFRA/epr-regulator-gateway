using System.Net;
using Xunit;

namespace EprRegulatorGateway.IntegrationTests.Integration;

[Trait("Category", "IntegrationTests")]
public sealed class AccountApiIntegrationTests : IClassFixture<GatewayWebApplicationFactory>
{
    private readonly GatewayWebApplicationFactory _factory;

    public AccountApiIntegrationTests(GatewayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_returns_ok_without_authorization()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Account_without_authorization_returns_unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            new Uri($"/api/account/{Guid.NewGuid():D}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Account_with_valid_jwt_returns_ok()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync(
            new Uri($"/api/account/{Guid.NewGuid():D}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

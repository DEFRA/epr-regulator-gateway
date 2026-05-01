using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EprRegulatorGateway.IntegrationTests.WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace EprRegulatorGateway.IntegrationTests.Integration;

[Trait("Category", "IntegrationTests")]
public sealed class AccountApiWireMockScenarioTests : IAsyncLifetime
{
    private WireMockContext? _wireMock;

    public ValueTask InitializeAsync()
    {
        _wireMock = new WireMockContext();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _wireMock?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task When_user_service_returns_ok_then_account_endpoint_returns_mapped_payload()
    {
        var userId = Guid.NewGuid();

        _wireMock!.Server
            .Given(
                Request
                    .Create()
                    .WithPath("/api/users/user-organisations")
                    .WithParam("userId", userId.ToString("D"))
                    .UsingGet()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        """
                        {
                          "user": {
                            "firstName": "Ada",
                            "lastName": "Lovelace",
                            "serviceRole": "Regulator",
                            "serviceRoleId": 123,
                            "email": "ada@example.test",
                            "organisations": [
                              { "id": "11111111-1111-1111-1111-111111111111", "name": "Org A", "nationId": 2 }
                            ]
                          }
                        }
                        """
                    )
            );

        using var factory = new GatewayWebApplicationFactory()
            .UseFakeAccountClient(false)
            .WithWebHostConfiguration(
                builder =>
                {
                    builder.UseSetting("UserService:BaseUrl", _wireMock.BaseAddress);
                    builder.UseSetting("UserService:Scope", "");
                });

        var client = factory.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            new Uri($"/api/account/{userId:D}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(userId.ToString("D"), json.GetProperty("userId").GetString());
        Assert.Equal("Ada", json.GetProperty("firstName").GetString());
        Assert.Equal("Lovelace", json.GetProperty("lastName").GetString());
        Assert.Equal("Org A", json.GetProperty("organisationName").GetString());
        Assert.Equal(2, json.GetProperty("nationId").GetInt32());
        Assert.Equal(123, json.GetProperty("serviceRoleId").GetInt32());
        Assert.Equal("Regulator", json.GetProperty("serviceRole").GetString());
        Assert.Equal("ada@example.test", json.GetProperty("contactEmail").GetString());
    }

    [Fact]
    public async Task When_user_service_returns_404_then_account_endpoint_returns_404_problem_details()
    {
        var userId = Guid.NewGuid();

        _wireMock!.Server
            .Given(
                Request
                    .Create()
                    .WithPath("/api/users/user-organisations")
                    .WithParam("userId", userId.ToString("D"))
                    .UsingGet()
            )
            .RespondWith(Response.Create().WithStatusCode(404).WithBody("not found"));

        using var factory = new GatewayWebApplicationFactory()
            .UseFakeAccountClient(false)
            .WithWebHostConfiguration(
                builder =>
                {
                    builder.UseSetting("UserService:BaseUrl", _wireMock.BaseAddress);
                    builder.UseSetting("UserService:Scope", "");
                });

        var client = factory.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            new Uri($"/api/account/{userId:D}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Not Found", json.GetProperty("title").GetString());
    }

    [Fact]
    public async Task When_user_service_returns_invalid_json_then_gateway_returns_500()
    {
        var userId = Guid.NewGuid();

        _wireMock!.Server
            .Given(
                Request
                    .Create()
                    .WithPath("/api/users/user-organisations")
                    .WithParam("userId", userId.ToString("D"))
                    .UsingGet()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{ this-is-not-valid-json }")
            );

        using var factory = new GatewayWebApplicationFactory()
            .UseFakeAccountClient(false)
            .WithWebHostConfiguration(
                builder =>
                {
                    builder.UseSetting("UserService:BaseUrl", _wireMock.BaseAddress);
                    builder.UseSetting("UserService:Scope", "");
                });

        var client = factory.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            new Uri($"/api/account/{userId:D}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}


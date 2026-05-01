using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EprRegulatorGateway.IntegrationTests.Setup;
using EprRegulatorGateway.IntegrationTests.Setup.WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace EprRegulatorGateway.IntegrationTests.Scenarios;

[Trait("Category", "IntegrationTests")]
public sealed class AccountScenarioTests : IAsyncLifetime
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

    private GatewayWebApplicationFactory CreateFactory()
    {
        return new GatewayWebApplicationFactory()
            .UseFakeAccountClient(false)
            .WithWebHostConfiguration(
                builder =>
                {
                    builder.UseSetting("UserService:BaseUrl", _wireMock!.BaseAddress);
                    builder.UseSetting("UserService:Scope", "");
                });
    }

    private void StubUserServiceResponse(Guid userId, int statusCode, string body, string? contentType = null)
    {
        var response = Response.Create().WithStatusCode(statusCode).WithBody(body);

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            response = response.WithHeader("Content-Type", contentType);
        }

        _wireMock!.Server
            .Given(
                Request
                    .Create()
                    .WithPath("/api/users/user-organisations")
                    .WithParam("userId", userId.ToString("D"))
                    .UsingGet()
            )
            .RespondWith(response);
    }

    private static async Task<HttpResponseMessage> GetAccount(HttpClient client, Guid userId)
    {
        return await client.GetAsync(
            new Uri($"/api/account/{userId:D}", UriKind.Relative),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_user_service_returns_ok_then_account_endpoint_returns_mapped_payload()
    {
        var userId = Guid.NewGuid();

        StubUserServiceResponse(
            userId,
            statusCode: 200,
            contentType: "application/json",
            body:
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
        );

        using var factory = CreateFactory();

        var client = factory.CreateAuthenticatedClient();

        using var response = await GetAccount(client, userId);

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

        StubUserServiceResponse(userId, statusCode: 404, body: "not found");

        using var factory = CreateFactory();

        var client = factory.CreateAuthenticatedClient();

        using var response = await GetAccount(client, userId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Not Found", json.GetProperty("title").GetString());
    }

    [Fact]
    public async Task When_user_service_returns_invalid_json_then_gateway_returns_500()
    {
        var userId = Guid.NewGuid();

        StubUserServiceResponse(
            userId,
            statusCode: 200,
            contentType: "application/json",
            body: "{ this-is-not-valid-json }");

        using var factory = CreateFactory();

        var client = factory.CreateAuthenticatedClient();

        using var response = await GetAccount(client, userId);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}


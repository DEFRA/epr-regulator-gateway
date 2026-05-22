using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EprRegulatorGateway.IntegrationTests.Setup;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace EprRegulatorGateway.IntegrationTests.Scenarios;

[Trait("Category", "IntegrationTests")]
public sealed class BackendAccountOutboundAuthenticationTests : IntegrationTestBase
{
    private const string StubClientId = "integration-test-client-id";
    private const string StubClientSecret = "integration-test-client-secret";
    private const string StubIssuedAccessToken = "stub-oauth-access-token";
    private const string TokenPath = "/token";

    protected override void ConfigureFactory(GatewayWebApplicationFactory factory)
    {
        base.ConfigureFactory(factory);

        factory.WithWebHostConfiguration(
            builder =>
            {
                builder.UseSetting("BackendAccountService:TokenEndpoint", $"{TokenServer.Url}{TokenPath}");
                builder.UseSetting("BackendAccountService:ClientId", StubClientId);
                builder.UseSetting("BackendAccountService:ClientSecret", StubClientSecret);
                builder.UseSetting("BackendAccountService:Scope", "api://stub-resource/.default");
            });
    }

    private void StubOAuthTokenSuccess()
    {
        var json = JsonSerializer.Serialize(
            new Dictionary<string, object>
            {
                ["access_token"] = StubIssuedAccessToken,
                ["expires_in"] = 3600,
            });

        TokenServer.Given(Request.Create().UsingPost().WithPath(TokenPath))
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(json));
    }

    private void StubOAuthTokenFailure(int statusCode, string body)
    {
        TokenServer.Given(Request.Create().UsingPost().WithPath(TokenPath))
            .RespondWith(Response.Create().WithStatusCode(statusCode).WithBody(body));
    }

    private static string UserOrganisationsJson(Guid userId)
    {
        var payload = new
        {
            user = new
            {
                firstName = "Stub",
                lastName = "User",
                serviceRole = "Regulator",
                serviceRoleId = 42,
                email = "stub.user@example.test",
                organisations = new[]
                {
                    new { id = userId.ToString("D"), name = "Stub Org", nationId = 99 },
                },
            },
        };

        return JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private void StubUserOrganisationsRequiringBearer(Guid userId, string requiredBearerToken)
    {
        BackendAccountServer.Given(
                Request.Create()
                    .UsingGet()
                    .WithPath("/api/users/user-organisations")
                    .WithParam("userId", userId.ToString("D"))
                    .WithHeader("Authorization", $"Bearer {requiredBearerToken}"))
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(UserOrganisationsJson(userId)));
    }

    [Fact]
    public async Task When_scope_configured_gateway_fetches_oauth_token_then_calls_backend_with_that_bearer()
    {
        var userId = Guid.Parse("4614e15e-b84d-4abf-b629-c25081a7dbcb");

        StubOAuthTokenSuccess();
        StubUserOrganisationsRequiringBearer(userId, StubIssuedAccessToken);

        var client = Factory.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            new Uri($"/api/account/{userId:D}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tokenRequests = TokenServer.LogEntries
            .Where(e => e.RequestMessage.Path == TokenPath)
            .ToList();
        Assert.Single(tokenRequests);

        var backendRequests = BackendAccountServer.LogEntries
            .Where(e => e.RequestMessage.Path == "/api/users/user-organisations")
            .ToList();
        Assert.Single(backendRequests);
        Assert.Equal(
            $"Bearer {StubIssuedAccessToken}",
            backendRequests[0].RequestMessage.Headers!["Authorization"].Single());
    }

    [Fact]
    public async Task When_token_endpoint_returns_unauthenticated_gateway_returns_bad_gateway_problem_details()
    {
        var userId = Guid.NewGuid();

        StubOAuthTokenFailure(401, "invalid_client");

        var client = Factory.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            new Uri($"/api/account/{userId:D}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Bad Gateway", json.GetProperty("title").GetString());
    }
}

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
public sealed class BackendAccountOutboundAuthenticationTests : IAsyncLifetime
{
    private WireMockContext? _wireMock;

    private const string StubClientId = "integration-test-client-id";
    private const string StubClientSecret = "integration-test-client-secret";

    private const string StubIssuedAccessToken = "stub-oauth-access-token";

    private static string BackendBase(WireMockContext wm) =>
        $"{wm.BaseAddress.TrimEnd('/')}/";

    private static string StubTokenEndpoint(WireMockContext wm) =>
        $"{wm.BaseAddress.TrimEnd('/')}/integration-oauth/token";

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

    private GatewayWebApplicationFactory CreateGatewayWithOutboundAuth()
    {
        return new GatewayWebApplicationFactory()
            .UseFakeAccountService(false)
            .WithWebHostConfiguration(
                builder =>
                {
                    builder.UseSetting("BackendAccountService:BaseUrl", BackendBase(_wireMock!));
                    builder.UseSetting("BackendAccountService:TokenEndpoint", StubTokenEndpoint(_wireMock!));
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

        _wireMock!.Server.Given(Request.Create().UsingPost().WithPath("/integration-oauth/token"))
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(json));
    }

    private void StubOAuthTokenFailure(int statusCode, string body)
    {
        _wireMock!.Server.Given(Request.Create().UsingPost().WithPath("/integration-oauth/token"))
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
        _wireMock!.Server.Given(
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
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        StubOAuthTokenSuccess();
        StubUserOrganisationsRequiringBearer(userId, StubIssuedAccessToken);

        await using var factory = CreateGatewayWithOutboundAuth();
        var client = factory.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            new Uri($"/api/account/{userId:D}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tokenRequests = _wireMock!.Server.LogEntries
            .Where(e => e.RequestMessage.Path == "/integration-oauth/token")
            .ToList();
        Assert.Single(tokenRequests);

        var backendRequests = _wireMock!.Server.LogEntries
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

        await using var factory = CreateGatewayWithOutboundAuth();
        var client = factory.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            new Uri($"/api/account/{userId:D}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Bad Gateway", json.GetProperty("title").GetString());
    }
}

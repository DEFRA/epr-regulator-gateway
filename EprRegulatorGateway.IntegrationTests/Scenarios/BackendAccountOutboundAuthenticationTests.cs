using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EprRegulatorGateway.IntegrationTests.Setup;
using EprRegulatorGateway.IntegrationTests.Setup.WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace EprRegulatorGateway.IntegrationTests.Scenarios;

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

    private static string UserOrganisationsJson(Guid userId)
    {
        var payload = new
        {
            user = new
            {
                firstName = "Token",
                lastName = "Flow",
                serviceRole = "Regulator",
                serviceRoleId = 42,
                email = "token-flow@example.test",
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

    private void StubUserOrganisationsSuccessfulWhenBearerMatches(Guid userId)
    {
        _wireMock!.Server.Given(
                Request.Create()
                    .UsingGet()
                    .WithPath("/api/users/user-organisations")
                    .WithParam("userId", userId.ToString("D"))
                    .WithHeader("Authorization", $"Bearer {StubIssuedAccessToken}"))
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(UserOrganisationsJson(userId)));
    }

    [Fact]
    public async Task When_scope_configured_gateway_fetches_oauth_token_then_calls_backend_api_with_matching_bearer_token()
    {
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        StubOAuthTokenSuccess();
        StubUserOrganisationsSuccessfulWhenBearerMatches(userId);

        await using var factory = CreateGatewayWithOutboundAuth();
        var client = factory.CreateAuthenticatedClient();

        using var response = await client.GetAsync(
            new Uri($"/api/account/{userId:D}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(userId.ToString("D"), json.GetProperty("userId").GetString());
        Assert.Equal("Token", json.GetProperty("firstName").GetString());
        Assert.Equal("Flow", json.GetProperty("lastName").GetString());
        Assert.Equal("Stub Org", json.GetProperty("organisationName").GetString());
        Assert.Equal(99, json.GetProperty("nationId").GetInt32());
    }

    [Fact]
    public async Task When_token_endpoint_returns_unauthenticated_gateway_returns_bad_gateway_problem_details()
    {
        var userId = Guid.NewGuid();

        _wireMock!.Server.Given(Request.Create().UsingPost().WithPath("/integration-oauth/token"))
            .RespondWith(Response.Create().WithStatusCode(401).WithBody("invalid_client"));

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

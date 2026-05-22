namespace EprRegulatorGateway.MockTokenServer.TokenApi;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

public static class TokenApi
{
    private const string TokenPath = "/token";
    private const string ResponsesRelativeDirectory = "Responses/TokenApi";
    private const string DefaultResponseFileName = "default.json";

    public static WireMockServer WithTokenApi(this WireMockServer server)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, ResponsesRelativeDirectory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directory}");
        }

        var defaultPath = Path.Combine(directory, DefaultResponseFileName);
        if (File.Exists(defaultPath))
        {
            server.Given(Request.Create()
                    .UsingPost()
                    .WithPath(TokenPath))
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyFromFile(defaultPath));
        }

        return server;
    }
}

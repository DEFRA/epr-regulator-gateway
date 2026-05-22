namespace EprRegulatorGateway.MockBackendAccount.BackendAccountApi;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

public static class BackendAccountApi
{
    private const string UserOrganisationsPath = "/api/users/user-organisations";
    private const string UserOrganisationsRelativeDirectory = "Responses/BackendAccountApi/UserOrganisations";
    private const string DefaultResponseFileName = "default.json";

    public static WireMockServer WithBackendAccountApi(this WireMockServer server)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, UserOrganisationsRelativeDirectory);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directory}");
        }

        foreach (var filePath in Directory.GetFiles(directory, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(filePath);

            if (string.Equals(name + ".json", DefaultResponseFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!Guid.TryParse(name, out var userId))
            {
                throw new InvalidOperationException($"File name is not a valid GUID: {filePath}");
            }

            server.Given(Request.Create()
                    .UsingGet()
                    .WithPath(UserOrganisationsPath)
                    .WithParam("userId", userId.ToString("D")))
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyFromFile(filePath));
        }

        var defaultPath = Path.Combine(directory, DefaultResponseFileName);
        if (File.Exists(defaultPath))
        {
            server.Given(Request.Create()
                    .UsingGet()
                    .WithPath(UserOrganisationsPath))
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyFromFile(defaultPath));
        }

        return server;
    }
}

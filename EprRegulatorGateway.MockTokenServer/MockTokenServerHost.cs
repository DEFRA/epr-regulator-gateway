namespace EprRegulatorGateway.MockTokenServer;

using EprRegulatorGateway.MockTokenServer.TokenApi;
using WireMock.Logging;
using WireMock.Net.StandAlone;
using WireMock.Server;
using WireMock.Settings;

public static class MockTokenServerHost
{
    public static WireMockServer Start(int? port = null, bool useSsl = false)
    {
        var settings = new WireMockServerSettings
        {
            UseSSL = useSsl,
            Logger = new WireMockConsoleLogger(),
        };

        if (port.HasValue)
        {
            settings.Port = port.Value;
        }

        return StandAloneApp.Start(settings).WithTokenApi();
    }
}

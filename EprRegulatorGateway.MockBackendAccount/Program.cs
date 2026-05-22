namespace EprRegulatorGateway.MockBackendAccount;

public static class Program
{
    private static void Main()
    {
        const int port = 8086;
        Console.WriteLine($"MockBackendAccount starting on http://localhost:{port}");
        MockBackendAccountServer.Start(port: port, useSsl: false);

        Console.WriteLine("Press any key to stop.");
        Console.ReadKey();
    }
}

namespace EprRegulatorGateway.MockTokenServer;

public static class Program
{
    private static void Main()
    {
        const int port = 8087;
        Console.WriteLine($"MockTokenServer starting on http://localhost:{port}");
        MockTokenServerHost.Start(port: port, useSsl: false);

        Console.WriteLine("Press any key to stop.");
        Console.ReadKey();
    }
}

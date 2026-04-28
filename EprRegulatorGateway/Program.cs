using EprRegulatorGateway.Utils;
using EprRegulatorGateway.Utils.Http;
using System.Diagnostics.CodeAnalysis;
using EprRegulatorGateway.Utils.Logging;
using EprRegulatorGateway.Account.Services;
using EprRegulatorGateway.Account.Handlers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;

var app = BuildApp(args);
await app.RunAsync();

[ExcludeFromCodeCoverage]
static WebApplication BuildApp(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    ConfigureHost(builder);
    ConfigureServices(builder);

    var app = builder.Build();

    ConfigureMiddleware(app);
    ConfigureEndpoints(app);

    return app;
}

[ExcludeFromCodeCoverage]
static void ConfigureHost(WebApplicationBuilder builder)
{
    builder.Host.UseSerilog(CdpLogging.Configuration);
}

[ExcludeFromCodeCoverage]
static void ConfigureServices(WebApplicationBuilder builder)
{
    var services = builder.Services;
    var configuration = builder.Configuration;

    // Trust material must be loaded before anything creates outbound connections.
    services.LoadCustomTrustStoreFromEnvironment();

    services.AddProblemDetails();
    services.AddValidation();
    services.AddControllers();

    services.AddHttpContextAccessor();

    ConfigureHeaderPropagation(services, configuration);
    ConfigureUserApi(services, configuration);

    services.AddHealthChecks();

    // App services
}

[ExcludeFromCodeCoverage]
static void ConfigureHeaderPropagation(IServiceCollection services, IConfiguration configuration)
{
    var traceHeader = configuration.GetValue<string>("TraceHeader");

    services.AddHeaderPropagation(options =>
    {
        if (!string.IsNullOrWhiteSpace(traceHeader))
        {
            options.Headers.Add(traceHeader);
        }
    });
}

[ExcludeFromCodeCoverage]
static void ConfigureUserApi(IServiceCollection services, IConfiguration configuration)
{
    services
        .AddOptions<UserServiceOptions>()
        .Bind(configuration.GetRequiredSection("UserService"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    services.AddTransient<UserServiceAuthorisationHandler>();

    services.AddHttpClientWithTracing<IUserApiClient, UserApiClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<UserServiceOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    })
    .AddHttpMessageHandler<UserServiceAuthorisationHandler>();

    services.AddScoped<IAccountClient, AccountClient>();
}

[ExcludeFromCodeCoverage]
static void ConfigureMiddleware(WebApplication app)
{
    app.UseSerilogRequestLogging();

    app.UseHeaderPropagation();
}

[ExcludeFromCodeCoverage]
static void ConfigureEndpoints(WebApplication app)
{
    app.MapHealthChecks("/health", new HealthCheckOptions());

    app.MapControllers();

    // Remove before deploying
}
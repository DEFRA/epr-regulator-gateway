using EprRegulatorGateway.Authentication;
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
    services.AddOpenApi();

    services.AddAuthenticationAuthorization(configuration);

    services.AddHttpContextAccessor();

    ConfigureHeaderPropagation(services, configuration);
    ConfigureBackendAccountService(services, configuration);

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
static void ConfigureBackendAccountService(IServiceCollection services, IConfiguration configuration)
{
    services
        .AddOptions<BackendAccountServiceOptions>()
        .Bind(configuration.GetRequiredSection("BackendAccountService"))
        .ValidateDataAnnotations()
        .Validate(
            options => string.IsNullOrWhiteSpace(options.Scope)
                       || Uri.TryCreate(options.TokenEndpoint!, UriKind.Absolute, out _)
                          && !string.IsNullOrWhiteSpace(options.ClientId)
                          && !string.IsNullOrWhiteSpace(options.ClientSecret),
            "BackendAccountService: TokenEndpoint (absolute URL), ClientId, and ClientSecret are required when Scope is configured.");

    services
        .AddOptions<ClientCredentialsTokenOptions>()
        .Bind(configuration.GetRequiredSection("BackendAccountService"))
        .ValidateDataAnnotations();

    services.AddSingleton<IClientCredentialsAccessTokenProvider, ClientCredentialsAccessTokenProvider>();
    services.AddTransient<BackendAccountAuthorisationHandler>();

    services.AddHttpClientWithTracing<IBackendAccountClient, BackendAccountClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<BackendAccountServiceOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    })
    .AddHttpMessageHandler<BackendAccountAuthorisationHandler>();

    services.AddScoped<IAccountService, AccountService>();
}

[ExcludeFromCodeCoverage]
static void ConfigureMiddleware(WebApplication app)
{
    app.UseSerilogRequestLogging();
    app.UseDownstreamExceptionHandling();

    app.UseHeaderPropagation();

    app.UseAuthentication();
    app.UseAuthorization();
}

[ExcludeFromCodeCoverage]
static void ConfigureEndpoints(WebApplication app)
{
    app.MapHealthChecks("/health", new HealthCheckOptions());

    app.MapOpenApi("/documentation/openapi/{documentName}.json");
    app.UseReDoc(options =>
    {
        options.RoutePrefix = "documentation";
        options.SpecUrl = "/documentation/openapi/v1.json";
    });

    app.MapControllers();

    // Remove before deploying
}

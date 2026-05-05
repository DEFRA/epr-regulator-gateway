using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EprRegulatorGateway.Utils.Auth;

public static class RejectAuthenticationDefaults
{
    public const string SchemeName = "Reject";
}

/// <summary>
/// Secure-by-default authentication scheme used when the real auth provider
/// (e.g. AzureAd / JWT bearer validation) is not configured.
/// </summary>
public sealed class RejectAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public RejectAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.Fail("Authentication is not configured."));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties? properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties? properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}


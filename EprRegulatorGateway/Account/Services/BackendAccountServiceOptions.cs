using System.ComponentModel.DataAnnotations;

namespace EprRegulatorGateway.Account.Services;

public sealed class BackendAccountServiceOptions
{
    [Required]
    [Url]
    public required string BaseUrl { get; init; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>OAuth2 token endpoint used for the client credentials flow.</summary>
    [Url]
    public string? TokenEndpoint { get; init; }

    public string? ClientId { get; init; }

    public string? ClientSecret { get; init; }

    /// <summary>
    /// Azure AD scope (for example resource App ID URI + '/.default'). When empty, no bearer token is sent to the backend.
    /// </summary>
    public string? Scope { get; init; }
}

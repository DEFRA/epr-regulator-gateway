using System.ComponentModel.DataAnnotations;

namespace EprRegulatorGateway.Account.Services;

/// <summary>Configuration for OAuth2 client credentials token acquisition.</summary>
public sealed class ClientCredentialsTokenOptions
{
    /// <summary>OAuth2 token endpoint URL.</summary>
    [Url]
    public string? TokenEndpoint { get; init; }

    public string? ClientId { get; init; }

    public string? ClientSecret { get; init; }

    /// <summary>When empty, outbound calls should not use this flow.</summary>
    public string? Scope { get; init; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}

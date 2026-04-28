using System.ComponentModel.DataAnnotations;

namespace EprRegulatorGateway.Account.Services;

public sealed class UserServiceOptions
{
    [Required]
    [Url]
    public required string BaseUrl { get; init; }

    /// <summary>
    /// Azure AD scope (or App ID URI + '/.default') used to obtain a token for the downstream User Service.
    /// If not set, no Authorization header will be added.
    /// </summary>
    public string? Scope { get; init; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}

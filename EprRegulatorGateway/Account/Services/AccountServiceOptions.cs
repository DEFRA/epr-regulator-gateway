using System.ComponentModel.DataAnnotations;

namespace EprRegulatorGateway.Account.Services;

public sealed class AccountServiceOptions
{
    [Required]
    [Url]
    public required string BaseUrl { get; init; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}


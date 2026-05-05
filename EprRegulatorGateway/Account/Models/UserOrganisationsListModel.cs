using System.Text.Json.Serialization;

namespace EprRegulatorGateway.Account.Models;

public sealed class UserOrganisationsListModel
{
    [JsonPropertyName("user")]
    public UserDetailsModel? User { get; init; }
}

public sealed class UserDetailsModel
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("serviceRole")]
    public string? ServiceRole { get; init; }

    [JsonPropertyName("serviceRoleId")]
    public int? ServiceRoleId { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("organisations")]
    public IReadOnlyList<OrganisationDetailModel>? Organisations { get; init; }
}

public sealed class OrganisationDetailModel
{
    [JsonPropertyName("id")]
    public Guid? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("nationId")]
    public int? NationId { get; init; }
}

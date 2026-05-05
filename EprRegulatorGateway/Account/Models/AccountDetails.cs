namespace EprRegulatorGateway.Account.Models;

public sealed record AccountDetails(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? OrganisationName,
    int? NationId,
    int? ServiceRoleId,
    string? ServiceRole,
    string? ContactEmail);

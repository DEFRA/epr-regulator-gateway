namespace EprRegulatorGateway.Account.Contracts.Responses;

public sealed record AccountDetailsResponse(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? OrganisationName,
    int? NationId,
    int? ServiceRoleId,
    string? ServiceRole,
    string? ContactEmail);


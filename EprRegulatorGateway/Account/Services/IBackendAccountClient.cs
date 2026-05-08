using EprRegulatorGateway.Account.Models;

namespace EprRegulatorGateway.Account.Services;

public interface IBackendAccountClient
{
    Task<UserOrganisationsListModel?> GetUserOrganisationsAsync(Guid userId, CancellationToken cancellationToken);
}

using EprRegulatorGateway.Account.Models;

namespace EprRegulatorGateway.Account.Services;

public interface IUserApiClient
{
    Task<UserOrganisationsListModel?> GetUserOrganisationsAsync(Guid userId, CancellationToken cancellationToken);
}

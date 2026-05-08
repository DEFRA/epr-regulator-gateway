using EprRegulatorGateway.Account.Contracts.Responses;

namespace EprRegulatorGateway.Account.Services;

public interface IAccountService
{
    Task<AccountDetailsResponse> GetAccountDetailsAsync(Guid userId, CancellationToken cancellationToken);
}

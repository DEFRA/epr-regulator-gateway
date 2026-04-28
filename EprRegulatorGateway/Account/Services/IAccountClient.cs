using EprRegulatorGateway.Account.Contracts.Responses;

namespace EprRegulatorGateway.Account.Services;

public interface IAccountClient
{
    Task<AccountDetailsResponse> GetAccountDetailsAsync(Guid userId, CancellationToken cancellationToken);
}


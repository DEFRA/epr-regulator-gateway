using EprRegulatorGateway.Account.Contracts.Responses;

namespace EprRegulatorGateway.Account.Services;

public sealed class AccountClient(IUserApiClient userApiClient) : IAccountClient
{
    public async Task<AccountDetailsResponse> GetAccountDetailsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var list = await userApiClient.GetUserOrganisationsAsync(userId, cancellationToken);

        if (list?.User is null)
        {
            throw new InvalidOperationException("User service returned an empty or invalid user-organisations response.");
        }

        var user = list.User;
        var org = user.Organisations?.FirstOrDefault();

        return new AccountDetailsResponse(
            userId,
            user.FirstName,
            user.LastName,
            org?.Name,
            org?.NationId,
            user.ServiceRoleId,
            ServiceRole: user.ServiceRole,
            user.Email);
    }
}
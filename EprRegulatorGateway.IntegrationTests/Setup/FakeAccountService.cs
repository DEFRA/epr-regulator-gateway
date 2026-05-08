using EprRegulatorGateway.Account.Contracts.Responses;
using EprRegulatorGateway.Account.Services;

namespace EprRegulatorGateway.IntegrationTests.Setup;

internal sealed class FakeAccountService : IAccountService
{
    public Task<AccountDetailsResponse> GetAccountDetailsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var response = new AccountDetailsResponse(
            userId,
            "Integration",
            "Test",
            "Test Org",
            NationId: 1,
            ServiceRoleId: 2,
            ServiceRole: "Regulator",
            ContactEmail: "integration@test.local");

        return Task.FromResult(response);
    }
}

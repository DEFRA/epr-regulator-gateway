namespace EprRegulatorGateway.Account.Services;

public interface IClientCredentialsAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using EprRegulatorGateway.Account.Services;
using Microsoft.Extensions.Options;

namespace EprRegulatorGateway.Account.Handlers;

[ExcludeFromCodeCoverage]
public sealed class BackendAccountAuthorisationHandler : DelegatingHandler
{
    private readonly BackendAccountServiceOptions _options;
    private readonly IClientCredentialsAccessTokenProvider _tokenProvider;

    public BackendAccountAuthorisationHandler(
        IOptions<BackendAccountServiceOptions> options,
        IClientCredentialsAccessTokenProvider tokenProvider)
    {
        _options = options.Value;
        _tokenProvider = tokenProvider;
    }

    private bool RequiresClientCredentials =>
        !string.IsNullOrWhiteSpace(_options.Scope);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (RequiresClientCredentials)
        {
            var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

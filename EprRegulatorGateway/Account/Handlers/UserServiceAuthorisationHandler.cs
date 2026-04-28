using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using EprRegulatorGateway.Account.Services;
using Microsoft.Identity.Web;

namespace EprRegulatorGateway.Account.Handlers;

[ExcludeFromCodeCoverage]
public sealed class UserServiceAuthorisationHandler : DelegatingHandler
{
    private readonly TokenRequestContext? _tokenRequestContext;
    private readonly DefaultAzureCredential? _credentials;

    public UserServiceAuthorisationHandler(IOptions<UserServiceOptions> options)
    {
        if (string.IsNullOrWhiteSpace(options.Value.Scope))
        {
            return;
        }

        _tokenRequestContext = new TokenRequestContext(new[] { options.Value.Scope });
        _credentials = new DefaultAzureCredential();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_credentials is not null && _tokenRequestContext is not null)
        {
            var tokenResult = await _credentials.GetTokenAsync(_tokenRequestContext.Value, cancellationToken);
            request.Headers.Authorization =
                new AuthenticationHeaderValue(Microsoft.Identity.Web.Constants.Bearer, tokenResult.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}


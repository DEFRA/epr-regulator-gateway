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
    private readonly ILogger<BackendAccountAuthorisationHandler> _logger;

    public BackendAccountAuthorisationHandler(
        IOptions<BackendAccountServiceOptions> options,
        IClientCredentialsAccessTokenProvider tokenProvider,
        ILogger<BackendAccountAuthorisationHandler> logger)
    {
        _options = options.Value;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    private bool RequiresClientCredentials =>
        !string.IsNullOrWhiteSpace(_options.Scope);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("AuthHandler entered. RequestUri={RequestUri}, RequiresClientCredentials={RequiresClientCredentials}", request.RequestUri, RequiresClientCredentials);

        if (RequiresClientCredentials)
        {
            _logger.LogInformation("AuthHandler awaiting token. RequestUri={RequestUri}", request.RequestUri);
            string token;
            try
            {
                token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthHandler token fetch threw. RequestUri={RequestUri}, CancellationRequested={CancellationRequested}", request.RequestUri, cancellationToken.IsCancellationRequested);
                throw;
            }
            _logger.LogInformation("AuthHandler token obtained. RequestUri={RequestUri}, TokenLength={TokenLength}", request.RequestUri, token.Length);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        _logger.LogInformation("AuthHandler calling base.SendAsync. RequestUri={RequestUri}", request.RequestUri);
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            _logger.LogInformation("AuthHandler base.SendAsync returned. RequestUri={RequestUri}, StatusCode={StatusCode}", request.RequestUri, (int)response.StatusCode);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuthHandler base.SendAsync threw. RequestUri={RequestUri}, CancellationRequested={CancellationRequested}", request.RequestUri, cancellationToken.IsCancellationRequested);
            throw;
        }
    }
}

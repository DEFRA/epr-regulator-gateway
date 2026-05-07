using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using EprRegulatorGateway.Account.Services;
using Microsoft.Extensions.Options;

namespace EprRegulatorGateway.Account.Handlers;

[ExcludeFromCodeCoverage]
public sealed class BackendAccountAuthorisationHandler : DelegatingHandler
{
    private static readonly System.Text.Json.JsonSerializerOptions TokenResponseJsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private readonly BackendAccountServiceOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _cachedAccessToken;
    private DateTimeOffset _cachedAccessTokenExpiresAt;

    public BackendAccountAuthorisationHandler(
        IOptions<BackendAccountServiceOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    private bool RequiresClientCredentials =>
        !string.IsNullOrWhiteSpace(_options.Scope);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (RequiresClientCredentials)
        {
            var token = await GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedAccessToken is not null && DateTimeOffset.UtcNow < _cachedAccessTokenExpiresAt)
        {
            return _cachedAccessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedAccessToken is not null && DateTimeOffset.UtcNow < _cachedAccessTokenExpiresAt)
            {
                return _cachedAccessToken;
            }

            using var tokenClient = _httpClientFactory.CreateClient();
            tokenClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 60));

            using var response = await tokenClient.PostAsync(
                _options.TokenEndpoint,
                new FormUrlEncodedContent(
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = _options.ClientId!,
                        ["client_secret"] = _options.ClientSecret!,
                        ["scope"] = _options.Scope!.Trim(),
                    }),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await System.Text.Json.JsonSerializer.DeserializeAsync<TokenEndpointResponse>(
                stream,
                TokenResponseJsonOptions,
                cancellationToken);

            if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                throw new InvalidOperationException("OAuth token response did not contain an access_token.");
            }

            var lifetimeSeconds = payload.ExpiresIn > 0 ? payload.ExpiresIn : 3600;
            var refreshSkewSeconds = Math.Min(60, lifetimeSeconds / 2);

            _cachedAccessToken = payload.AccessToken;
            _cachedAccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(lifetimeSeconds - refreshSkewSeconds);

            return _cachedAccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private sealed record TokenEndpointResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

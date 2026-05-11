using System.Text.Json;
using EprRegulatorGateway.Account.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace EprRegulatorGateway.Account.Services;

public sealed class BackendAccountClient(HttpClient httpClient, ILogger<BackendAccountClient> logger) : IBackendAccountClient
{
    public async Task<UserOrganisationsListModel?> GetUserOrganisationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var path = QueryHelpers.AddQueryString(
            "api/users/user-organisations",
            new Dictionary<string, string?>
            {
                ["userId"] = userId.ToString("D")
            });

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Backend account outbound GET failed before response. Path={path}, HttpClientTimeoutSeconds={httpClient.Timeout.TotalSeconds}, CancellationRequested={cancellationToken.IsCancellationRequested}",
                ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var snippet = body.Length <= 2048 ? body : body[..2048];

                logger.LogWarning(
                    "Backend account HTTP error response. FailureKind={FailureKind}, HttpStatus={HttpStatusCode}, Method={Method}, Path={Path}, BodySnippet={BodySnippet}",
                    "http_error_status",
                    (int)response.StatusCode,
                    "GET",
                    path,
                    snippet);

                throw new HttpRequestException(
                    $"Backend account HTTP call failed with {(int)response.StatusCode} ({response.StatusCode}) for GET {response.RequestMessage?.RequestUri}. Body (first 2048 chars): {snippet}",
                    inner: null,
                    statusCode: response.StatusCode);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent || response.Content.Headers.ContentLength is 0)
            {
                logger.LogWarning(
                    "Backend account service returned empty body for GET {Uri}",
                    response.RequestMessage?.RequestUri);
                return null;
            }

            try
            {
                var model = await response.Content.ReadFromJsonAsync<UserOrganisationsListModel>(cancellationToken: cancellationToken);
                if (model?.User is null)
                {
                    logger.LogWarning(
                        "Backend account service returned an unexpected payload for GET {Uri} (missing 'user')",
                        response.RequestMessage?.RequestUri);
                }

                return model;
            }
            catch (JsonException ex)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var snippet = body.Length <= 2048 ? body : body[..2048];
                throw new JsonException(
                    $"Backend account service returned invalid JSON for GET {response.RequestMessage?.RequestUri}. Body (first 2048 chars): {snippet}",
                    ex);
            }
        }
    }
}

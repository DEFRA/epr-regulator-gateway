using System.Text.Json;
using EprRegulatorGateway.Account.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace EprRegulatorGateway.Account.Services;

public sealed class UserApiClient(HttpClient httpClient, ILogger<UserApiClient> logger) : IUserApiClient
{
    public async Task<UserOrganisationsListModel?> GetUserOrganisationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var path = QueryHelpers.AddQueryString(
            "api/users/user-organisations",
            new Dictionary<string, string?>
            {
                ["userId"] = userId.ToString("D")
            });

        using var response = await httpClient.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var snippet = body.Length <= 2048 ? body : body[..2048];

            throw new HttpRequestException(
                $"UserService call failed with {(int)response.StatusCode} ({response.StatusCode}) for GET {response.RequestMessage?.RequestUri}. Body (first 2048 chars): {snippet}",
                inner: null,
                statusCode: response.StatusCode);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent || response.Content.Headers.ContentLength is 0)
        {
            logger.LogWarning(
                "UserService call returned empty body for GET {Uri}",
                response.RequestMessage?.RequestUri);
            return null;
        }

        try
        {
            var model = await response.Content.ReadFromJsonAsync<UserOrganisationsListModel>(cancellationToken: cancellationToken);
            if (model?.User is null)
            {
                logger.LogWarning(
                    "UserService call returned an unexpected payload for GET {Uri} (missing 'user')",
                    response.RequestMessage?.RequestUri);
            }

            return model;
        }
        catch (JsonException ex)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var snippet = body.Length <= 2048 ? body : body[..2048];
            throw new JsonException(
                $"UserService returned invalid JSON for GET {response.RequestMessage?.RequestUri}. Body (first 2048 chars): {snippet}",
                ex);
        }
    }
}

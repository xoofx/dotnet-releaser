using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetReleaser.Configuration;
using DotNetReleaser.Helpers;

namespace DotNetReleaser;

internal sealed class NuGetTrustedPublishingClient
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public NuGetTrustedPublishingClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<string> ExchangeForApiKeyAsync(NuGetPublisher configuration, string userAgent)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(userAgent);

        if (string.IsNullOrWhiteSpace(configuration.User))
        {
            throw new InvalidOperationException("NuGet trusted publishing requires `nuget.user` to be configured with the NuGet username that created the trusted publishing policy.");
        }

        var oidcRequestToken = Environment.GetEnvironmentVariable("ACTIONS_ID_TOKEN_REQUEST_TOKEN");
        var oidcRequestUrl = Environment.GetEnvironmentVariable("ACTIONS_ID_TOKEN_REQUEST_URL");

        if (string.IsNullOrEmpty(oidcRequestToken) && string.IsNullOrEmpty(oidcRequestUrl))
        {
            throw new InvalidOperationException(
                "GitHub OIDC is not available. Ensure your workflow has the required permissions:" + Environment.NewLine +
                "  permissions:" + Environment.NewLine +
                "    id-token: write" + Environment.NewLine +
                "    contents: read");
        }

        if (string.IsNullOrEmpty(oidcRequestToken))
        {
            throw new InvalidOperationException(
                "ACTIONS_ID_TOKEN_REQUEST_TOKEN is missing. Ensure your workflow has:" + Environment.NewLine +
                "  permissions:" + Environment.NewLine +
                "    id-token: write");
        }

        if (string.IsNullOrEmpty(oidcRequestUrl))
        {
            throw new InvalidOperationException(
                "ACTIONS_ID_TOKEN_REQUEST_URL is missing. Ensure your workflow has:" + Environment.NewLine +
                "  permissions:" + Environment.NewLine +
                "    id-token: write");
        }

        GitHubActionHelper.MaskSecret(oidcRequestToken);

        using var oidcRequest = new HttpRequestMessage(HttpMethod.Get, BuildOidcTokenUrl(oidcRequestUrl, configuration.TrustedPublishingAudience));
        oidcRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oidcRequestToken);
        oidcRequest.Headers.UserAgent.ParseAdd(userAgent);

        using var oidcResponse = await _httpClient.SendAsync(oidcRequest).ConfigureAwait(false);
        var oidcBody = await oidcResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!oidcResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to retrieve OIDC token from GitHub (HTTP {(int)oidcResponse.StatusCode}). Verify that the audience is correct.");
        }

        var oidcToken = JsonSerializer.Deserialize<OidcTokenResponse>(oidcBody, JsonSerializerOptions)?.Value;
        if (string.IsNullOrWhiteSpace(oidcToken))
        {
            throw new InvalidOperationException($"Failed to retrieve OIDC token from GitHub (HTTP {(int)oidcResponse.StatusCode}). Verify that the audience is correct.");
        }

        GitHubActionHelper.MaskSecret(oidcToken);

        using var exchangeRequest = new HttpRequestMessage(HttpMethod.Post, configuration.TrustedPublishingTokenServiceUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(new TokenExchangeRequest(configuration.User, "ApiKey"), JsonSerializerOptions), Encoding.UTF8, "application/json")
        };
        exchangeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oidcToken);
        exchangeRequest.Headers.UserAgent.ParseAdd(userAgent);

        using var exchangeResponse = await _httpClient.SendAsync(exchangeRequest).ConfigureAwait(false);
        var exchangeBody = await exchangeResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!exchangeResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Token exchange failed (HTTP {(int)exchangeResponse.StatusCode}) at {configuration.TrustedPublishingTokenServiceUrl}. Make sure you are using the username of the policy creator, not the policy owner: {GetErrorMessage(exchangeBody)}");
        }

        var apiKey = JsonSerializer.Deserialize<TokenExchangeResponse>(exchangeBody, JsonSerializerOptions)?.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("NuGet trusted publishing token exchange response did not contain `apiKey`.");
        }

        GitHubActionHelper.MaskSecret(apiKey);
        return apiKey;
    }

    internal static string BuildOidcTokenUrl(string oidcRequestUrl, string audience)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oidcRequestUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        var separator = oidcRequestUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{oidcRequestUrl}{separator}audience={Uri.EscapeDataString(audience)}";
    }

    private static string GetErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return "empty response";
        }

        try
        {
            var error = JsonSerializer.Deserialize<TokenExchangeErrorResponse>(responseBody, JsonSerializerOptions)?.Error;
            if (!string.IsNullOrWhiteSpace(error))
            {
                return error;
            }
        }
        catch (JsonException)
        {
        }

        return responseBody;
    }

    private sealed record OidcTokenResponse(string? Value);

    private sealed record TokenExchangeRequest(string Username, string TokenType);

    private sealed record TokenExchangeResponse(string? ApiKey);

    private sealed record TokenExchangeErrorResponse(string? Error);
}

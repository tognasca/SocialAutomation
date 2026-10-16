using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;
using SocialAutomation.Domain.Models.Instagram;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SocialAutomation.Infrastructure.Instagram;

public class InstagramClient : IInstagramClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InstagramClient> _logger;
    private readonly IIntegrationLogRepository _integrationLogRepository;

    private readonly string _appId;
    private readonly string _appSecret;
    private const string GraphApiVersion = "v21.0";
    private const string GraphApiBaseUrl = "https://graph.facebook.com";

    public InstagramClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<InstagramClient> logger,
        IIntegrationLogRepository integrationLogRepository)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _integrationLogRepository = integrationLogRepository;

        _appId = _configuration["Instagram:AppId"] ?? throw new InvalidOperationException("Instagram:AppId not configured.");
        _appSecret = _configuration["Instagram:AppSecret"] ?? throw new InvalidOperationException("Instagram:AppSecret not configured.");
    }

    public string GetAuthorizationUrl(string state, string redirectUri)
    {
        var scopes = new[]
        {
            "pages_show_list",
            "pages_read_engagement",
            "pages_manage_metadata",
            "pages_manage_posts",
            "instagram_basic",
            "instagram_manage_messages",
            "instagram_manage_comments",
            "instagram_content_publish",
            "pages_manage_posts",
        };

        var scopeString = Uri.EscapeDataString(string.Join(",", scopes));
        var encodedRedirect = Uri.EscapeDataString(redirectUri);

        return $"https://www.facebook.com/{GraphApiVersion}/dialog/oauth?client_id={_appId}&redirect_uri={encodedRedirect}&scope={scopeString}&response_type=code&state={state}";
    }

    public async Task<MetaOAuthTokenResponse?> ExchangeCodeForTokenAsync(string code, string redirectUri, CancellationToken ct = default)
    {
        var url = $"{GraphApiBaseUrl}/{GraphApiVersion}/oauth/access_token" +
                  $"?client_id={_appId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&client_secret={_appSecret}" +
                  $"&code={Uri.EscapeDataString(code)}";

        return await ExecuteWithLoggingAsync<MetaOAuthTokenResponse>(HttpMethod.Get, url, "ExchangeCodeForToken", null, null, ct);
    }

    public async Task<MetaOAuthTokenResponse?> GetLongLivedUserTokenAsync(string shortLivedToken, CancellationToken ct = default)
    {
        var url = $"{GraphApiBaseUrl}/{GraphApiVersion}/oauth/access_token" +
                  $"?grant_type=fb_exchange_token" +
                  $"&client_id={_appId}" +
                  $"&client_secret={_appSecret}" +
                  $"&fb_exchange_token={shortLivedToken}";

        return await ExecuteWithLoggingAsync<MetaOAuthTokenResponse>(HttpMethod.Get, url, "GetLongLivedUserToken", null, null, ct);
    }

    public async Task<List<MetaPageData>> GetUserPagesWithInstagramAccountsAsync(string userAccessToken, CancellationToken ct = default)
    {
        var fields = "id,name,access_token,instagram_business_account{id,username,name,profile_picture_url}";
        var url = $"{GraphApiBaseUrl}/{GraphApiVersion}/me/accounts?fields={fields}&access_token={userAccessToken}";

        var response = await ExecuteWithLoggingAsync<MetaAccountsResponse>(HttpMethod.Get, url, "GetUserPagesWithInstagram", null, null, ct);
        return response?.Data ?? new List<MetaPageData>();
    }

    public async Task<bool> SubscribeAppToPageWebhooksAsync(string pageId, string pageAccessToken, CancellationToken ct = default)
    {
        var url = $"{GraphApiBaseUrl}/{GraphApiVersion}/{pageId}/subscribed_apps?subscribed_fields=messages,message_deliveries,message_reads,message_reactions,feed&access_token={pageAccessToken}";

        var response = await ExecuteWithLoggingAsync<JsonDocument>(HttpMethod.Post, url, "SubscribeAppToPageWebhooks", null, null, ct);
        if (response != null && response.RootElement.TryGetProperty("success", out var successEl))
        {
            return successEl.GetBoolean();
        }

        return false;
    }

    public async Task<MetaSendMessageResponse?> SendTextMessageAsync(
        string pageId,
        string pageAccessToken,
        string recipientInstagramScopedId,
        string text,
        CancellationToken ct = default)
    {
        var url = $"{GraphApiBaseUrl}/{GraphApiVersion}/{pageId}/messages?access_token={pageAccessToken}";

        var payload = new
        {
            recipient = new { id = recipientInstagramScopedId },
            message = new { text }
        };

        var jsonContent = JsonSerializer.Serialize(payload);
        return await ExecuteWithLoggingAsync<MetaSendMessageResponse>(HttpMethod.Post, url, "SendTextMessage", jsonContent, null, ct);
    }

    public async Task<MetaUserProfileResponse?> GetInstagramUserProfileAsync(
        string instagramScopedId,
        string pageAccessToken,
        CancellationToken ct = default)
    {
        var url = $"{GraphApiBaseUrl}/{GraphApiVersion}/{instagramScopedId}?fields=id,username,name,profile_pic&access_token={pageAccessToken}";
        return await ExecuteWithLoggingAsync<MetaUserProfileResponse>(HttpMethod.Get, url, "GetInstagramUserProfile", null, null, ct);
    }

    private async Task<T?> ExecuteWithLoggingAsync<T>(
        HttpMethod method,
        string url,
        string operationName,
        string? jsonPayload,
        Guid? tenantId,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var log = new IntegrationLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Provider = "meta_instagram",
            Operation = operationName,
            Endpoint = CleanUrlForLogging(url),
            HttpMethod = method.Method,
            RequestPayload = jsonPayload,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (jsonPayload != null)
            {
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            }

            using var response = await _httpClient.SendAsync(request, ct);
            stopwatch.Stop();
            log.DurationMs = stopwatch.ElapsedMilliseconds;
            log.StatusCode = (int)response.StatusCode;

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            log.ResponsePayload = CleanResponseForLogging(responseBody);
            log.Success = response.IsSuccessStatusCode;

            if (!response.IsSuccessStatusCode)
            {
                log.ErrorMessage = responseBody;
                _logger.LogWarning("Meta API call failed [{Operation}]: {StatusCode} - {Body}", operationName, response.StatusCode, responseBody);
                await _integrationLogRepository.CreateAsync(log, ct);
                return default;
            }

            await _integrationLogRepository.CreateAsync(log, ct);
            return JsonSerializer.Deserialize<T>(responseBody);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            log.DurationMs = stopwatch.ElapsedMilliseconds;
            log.Success = false;
            log.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Exception during Meta API call [{Operation}]", operationName);
            await _integrationLogRepository.CreateAsync(log, ct);
            return default;
        }
    }

    private static string CleanUrlForLogging(string url)
    {
        var cleaned = System.Text.RegularExpressions.Regex.Replace(url, @"access_token=[^&]+", "access_token=REDACTED");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"client_secret=[^&]+", "client_secret=REDACTED");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"fb_exchange_token=[^&]+", "fb_exchange_token=REDACTED");
        return cleaned;
    }

    private static string CleanResponseForLogging(string response)
    {
        return response.Length > 2000 ? response[..2000] + "... [truncated]" : response;
    }
}
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
        _logger = logger;
        _integrationLogRepository = integrationLogRepository;

        _appId = configuration["Instagram:AppId"] ?? string.Empty;
        _appSecret = configuration["Instagram:AppSecret"] ?? string.Empty;
    }

    public string GetAuthorizationUrl(string state, string redirectUri)
    {
        var scopes = new[]
        {
            "pages_show_list",
            "pages_read_engagement",
            "pages_manage_metadata",
            "pages_messaging",
            "business_management",
            "instagram_basic",
            "instagram_manage_messages",
            "instagram_manage_comments",
            "instagram_content_publish",
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
                  $"&client_secret={Uri.EscapeDataString(_appSecret)}" +
                  $"&code={Uri.EscapeDataString(code)}";

        return await ExecuteWithLoggingAsync<MetaOAuthTokenResponse>(HttpMethod.Get, url, "ExchangeCodeForToken", null, null, ct);
    }

    public async Task<MetaOAuthTokenResponse?> GetLongLivedUserTokenAsync(string shortLivedToken, CancellationToken ct = default)
    {
        var url = $"{GraphApiBaseUrl}/{GraphApiVersion}/oauth/access_token" +
                  $"?grant_type=fb_exchange_token" +
                  $"&client_id={_appId}" +
                  $"&client_secret={Uri.EscapeDataString(_appSecret)}" +
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
        var url = $"{GraphApiBaseUrl}/{GraphApiVersion}/{pageId}/subscribed_apps?subscribed_fields=messages,message_deliveries,message_reads&access_token={pageAccessToken}";

        using var response = await ExecuteWithLoggingAsync<JsonDocument>(HttpMethod.Post, url, "SubscribeAppToPageWebhooks", null, null, ct);
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
        var url = $"{GraphApiBaseUrl}/{GraphApiVersion}/{pageId}/messages";

        var payload = new
        {
            messaging_type = "RESPONSE",
            recipient = new { id = recipientInstagramScopedId },
            message = new { text }
        };

        var jsonContent = JsonSerializer.Serialize(payload);
        return await ExecuteWithLoggingAsync<MetaSendMessageResponse>(HttpMethod.Post, url, "SendTextMessage", jsonContent, null, ct, pageAccessToken);
    }

    public Task<MetaSendMessageResponse?> SendPrivateReplyAsync(
        string pageId, string pageAccessToken, string commentId, string text, CancellationToken ct)
    {
        var url = $"{GraphApiBaseUrl}/{GraphApiVersion}/{pageId}/messages";
        var payload = JsonSerializer.Serialize(new { recipient = new { comment_id = commentId }, message = new { text } });
        return ExecuteWithLoggingAsync<MetaSendMessageResponse>(
            HttpMethod.Post, url, "SendPrivateReply", payload, null, ct, pageAccessToken);
    }

    public async Task<MetaUserProfileResponse?> GetInstagramUserProfileAsync(
        string instagramScopedId,
        string pageAccessToken,
        CancellationToken ct = default)
    {
        var url = $"{GraphApiBaseUrl}/{GraphApiVersion}/{instagramScopedId}?fields=id,username,name,profile_pic&access_token={pageAccessToken}";
        return await ExecuteWithLoggingAsync<MetaUserProfileResponse>(HttpMethod.Get, url, "GetInstagramUserProfile", null, null, ct);
    }

    public async Task<JsonElement?> SearchAsync(string businessId, string token, string query, bool hashtag, CancellationToken ct)
    {
        var graph = $"{GraphApiBaseUrl}/{GraphApiVersion}";
        if (!hashtag)
        {
            var fields = Uri.EscapeDataString($"business_discovery.username({query}){{username,name,biography,website,profile_picture_url,followers_count,media_count}}");
            var profile = await ReadGraphAsync(HttpMethod.Get, $"{graph}/{businessId}?fields={fields}", token, null, ct);
            return profile.TryGetProperty("business_discovery", out var result) ? result : null;
        }

        var tags = await ReadGraphAsync(HttpMethod.Get,
            $"{graph}/ig_hashtag_search?user_id={businessId}&q={Uri.EscapeDataString(query)}", token, null, ct);
        if (tags.GetProperty("data").GetArrayLength() == 0)
            return null;

        var tagId = tags.GetProperty("data")[0].GetProperty("id").GetString();
        var media = await ReadGraphAsync(HttpMethod.Get,
            $"{graph}/{tagId}/top_media?user_id={businessId}&fields=id,caption,media_type,permalink,like_count,comments_count&limit=12",
            token, null, ct);
        return media.GetProperty("data");
    }

    public async Task<string> PublishAsync(string businessId, string token, string type, string mediaUrl, string caption, CancellationToken ct)
    {
        var graph = $"{GraphApiBaseUrl}/{GraphApiVersion}";
        var options = new Dictionary<string, string>
        {
            [type == "Reel" ? "video_url" : "image_url"] = mediaUrl
        };

        if (type != "Story")
            options["caption"] = caption;
        if (type == "Reel")
            options["media_type"] = "REELS";
        if (type == "Story")
            options["media_type"] = "STORIES";

        var id = await CreateContainerAsync(graph, businessId, token, options, ct);
        return await PublishContainerAsync(graph, businessId, token, id, ct);
    }

    public async Task<string> PublishCarouselAsync(string businessId, string token, IReadOnlyList<string> mediaUrls, string caption, CancellationToken ct)
    {
        var graph = $"{GraphApiBaseUrl}/{GraphApiVersion}";
        var children = new List<string>(mediaUrls.Count);

        foreach (var url in mediaUrls)
        {
            var child = await CreateContainerAsync(graph, businessId, token, new()
            {
                ["image_url"] = url,
                ["is_carousel_item"] = "true"
            }, ct);
            await WaitForContainerAsync(graph, token, child, ct);
            children.Add(child);
        }

        var id = await CreateContainerAsync(graph, businessId, token, new()
        {
            ["media_type"] = "CAROUSEL",
            ["children"] = string.Join(",", children),
            ["caption"] = caption
        }, ct);
        return await PublishContainerAsync(graph, businessId, token, id, ct);
    }

    private async Task<string> CreateContainerAsync(string graph, string businessId, string token,
        Dictionary<string, string> options, CancellationToken ct)
    {
        var result = await ReadGraphAsync(HttpMethod.Post, $"{graph}/{businessId}/media", token, options, ct);
        return result.GetProperty("id").GetString() ?? throw new InvalidOperationException("A Meta não retornou o ID da mídia.");
    }

    private async Task<string> PublishContainerAsync(string graph, string businessId, string token, string id, CancellationToken ct)
    {
        await WaitForContainerAsync(graph, token, id, ct);
        var result = await ReadGraphAsync(HttpMethod.Post, $"{graph}/{businessId}/media_publish", token,
            new() { ["creation_id"] = id }, ct);
        return result.GetProperty("id").GetString() ?? throw new InvalidOperationException("A Meta não retornou o ID da publicação.");
    }

    private async Task WaitForContainerAsync(string graph, string token, string id, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var status = await ReadGraphAsync(HttpMethod.Get, $"{graph}/{id}?fields=status_code", token, null, ct);
            var code = status.GetProperty("status_code").GetString();
            if (code == "FINISHED")
                return;

            if (code is "ERROR" or "EXPIRED")
                throw new InvalidOperationException($"A Meta encerrou o processamento da mídia com estado {code}.");

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }

        throw new InvalidOperationException("A mídia não ficou pronta no Instagram dentro do prazo.");
    }

    private async Task<JsonElement> ReadGraphAsync(HttpMethod method, string url, string token,
        Dictionary<string, string>? fields, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (fields is not null)
            request.Content = new FormUrlEncodedContent(fields);

        using var response = await _httpClient.SendAsync(request, ct);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (!response.IsSuccessStatusCode)
        {
            var code = document.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("code", out var value)
                ? value.ToString() : "indisponível";
            throw new InvalidOperationException($"A Meta rejeitou a operação (HTTP {(int)response.StatusCode}, código {code}).");
        }

        return document.RootElement.Clone();
    }

    private async Task<T?> ExecuteWithLoggingAsync<T>(
        HttpMethod method,
        string url,
        string operationName,
        string? jsonPayload,
        Guid? tenantId,
        CancellationToken ct,
        string? bearerToken = null)
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
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (bearerToken is not null)
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
            if (jsonPayload != null)
            {
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            }

            using var response = await _httpClient.SendAsync(request, ct);
            stopwatch.Stop();
            log.DurationMs = stopwatch.ElapsedMilliseconds;
            log.StatusCode = (int)response.StatusCode;

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            log.Success = response.IsSuccessStatusCode;

            if (!response.IsSuccessStatusCode)
            {
                log.ErrorMessage = $"Meta HTTP {(int)response.StatusCode}";
                _logger.LogWarning("Meta API call failed [{Operation}]: {StatusCode}", operationName, response.StatusCode);
                await WriteIntegrationLogAsync(log, ct);
                return default;
            }

            await WriteIntegrationLogAsync(log, ct);
            return JsonSerializer.Deserialize<T>(responseBody);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            log.DurationMs = stopwatch.ElapsedMilliseconds;
            log.Success = false;
            log.ErrorMessage = ex.GetType().Name;

            _logger.LogError("Exception {ErrorType} during Meta API call [{Operation}]", ex.GetType().Name, operationName);
            await WriteIntegrationLogAsync(log, ct);
            return default;
        }
    }

    private async Task WriteIntegrationLogAsync(IntegrationLog log, CancellationToken ct)
    {
        try
        {
            await _integrationLogRepository.CreateAsync(log, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Falha ao gravar metadado da chamada à Meta: {ErrorType}", ex.GetType().Name);
        }
    }

    private static string CleanUrlForLogging(string url)
    {
        var cleaned = System.Text.RegularExpressions.Regex.Replace(url, @"access_token=[^&]+", "access_token=REDACTED");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"client_secret=[^&]+", "client_secret=REDACTED");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"fb_exchange_token=[^&]+", "fb_exchange_token=REDACTED");
        return System.Text.RegularExpressions.Regex.Replace(cleaned, @"([?&])code=[^&]+", "$1code=REDACTED");
    }

}

using System.Text.Json;

using SocialAutomation.Domain.Models.Instagram;

namespace SocialAutomation.Domain.Interfaces.Services;

public interface IInstagramClient
{
    string GetAuthorizationUrl(string state, string redirectUri);
    Task<MetaOAuthTokenResponse?> ExchangeCodeForTokenAsync(string code, string redirectUri, CancellationToken ct = default);
    Task<MetaOAuthTokenResponse?> GetLongLivedUserTokenAsync(string shortLivedToken, CancellationToken ct = default);
    Task<List<MetaPageData>> GetUserPagesWithInstagramAccountsAsync(string userAccessToken, CancellationToken ct = default);
    Task<bool> SubscribeAppToPageWebhooksAsync(string pageId, string pageAccessToken, CancellationToken ct = default);
    Task<MetaSendMessageResponse?> SendTextMessageAsync(string pageId, string pageAccessToken, string recipientInstagramScopedId, string text, CancellationToken ct = default);
    Task<MetaSendMessageResponse?> SendPrivateReplyAsync(string pageId, string pageAccessToken, string commentId, string text, CancellationToken ct);
    Task<MetaUserProfileResponse?> GetInstagramUserProfileAsync(string instagramScopedId, string pageAccessToken, CancellationToken ct = default);
    Task<JsonElement?> SearchAsync(string businessId, string token, string query, bool hashtag, CancellationToken ct);
    Task<string> PublishAsync(string businessId, string token, string type, string mediaUrl, string caption, CancellationToken ct);
}

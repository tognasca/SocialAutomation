
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
    Task<MetaUserProfileResponse?> GetInstagramUserProfileAsync(string instagramScopedId, string pageAccessToken, CancellationToken ct = default);
}
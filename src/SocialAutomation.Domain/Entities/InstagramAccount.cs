
using SocialAutomation.Domain.Enums;

namespace SocialAutomation.Domain.Entities;

public class InstagramAccount
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string InstagramBusinessId { get; set; } = string.Empty;
    public string? InstagramUserId { get; set; }
    public string PageId { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public DateTime? TokenExpiresAt { get; set; }
    public InstagramAccountStatus Status { get; set; } = InstagramAccountStatus.Active;
    public bool WebhookSubscribed { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
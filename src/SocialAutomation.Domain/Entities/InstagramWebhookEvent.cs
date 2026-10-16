
using SocialAutomation.Domain.Enums;

namespace SocialAutomation.Domain.Entities;

public class InstagramWebhookEvent
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? InstagramAccountId { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public string EntryId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public WebhookEventStatus Status { get; set; } = WebhookEventStatus.Received;
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
namespace SocialAutomation.Domain.Enums;

public enum WebhookEventStatus : short
{
    Received = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4,
    Ignored = 5
}
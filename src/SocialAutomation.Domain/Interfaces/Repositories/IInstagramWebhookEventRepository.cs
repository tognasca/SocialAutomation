
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Enums;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IInstagramWebhookEventRepository
{
    Task<bool> TryCreateAsync(InstagramWebhookEvent webhookEvent, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, WebhookEventStatus status, string? errorMessage = null, CancellationToken ct = default);
    Task<InstagramWebhookEvent?> ClaimNextAsync(CancellationToken ct = default);
}
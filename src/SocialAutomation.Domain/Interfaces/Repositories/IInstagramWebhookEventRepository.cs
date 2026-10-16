
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Enums;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IInstagramWebhookEventRepository
{
    Task<bool> ExistsAsync(string eventId, CancellationToken ct = default);
    Task<Guid> CreateAsync(InstagramWebhookEvent webhookEvent, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, WebhookEventStatus status, string? errorMessage = null, CancellationToken ct = default);
    Task<IEnumerable<InstagramWebhookEvent>> GetPendingEventsAsync(int limit = 50, CancellationToken ct = default);
}
using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IContentRepository
{
    Task<IEnumerable<ContentItem>> GetAllByTenantAsync(Guid tenantId, string? type = null, string? status = null, CancellationToken ct = default);
    Task<ContentItem?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Guid> CreateAsync(ContentItem item, CancellationToken ct = default);
    Task UpdateAsync(ContentItem item, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, Guid tenantId, string status, string? publicationId = null, string? errorMessage = null, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    // Scenes for Reels
    Task SaveReelScenesAsync(Guid contentItemId, Guid tenantId, IEnumerable<ReelScene> scenes, CancellationToken ct = default);
    Task<IEnumerable<ReelScene>> GetScenesByContentIdAsync(Guid contentItemId, CancellationToken ct = default);

    // Worker queries
    Task<IEnumerable<ContentItem>> GetPendingScheduledContentAsync(int limit = 20, CancellationToken ct = default);
}
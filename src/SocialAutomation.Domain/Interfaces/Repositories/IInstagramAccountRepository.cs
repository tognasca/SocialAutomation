
using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IInstagramAccountRepository
{
    Task<InstagramAccount?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<InstagramAccount?> GetByBusinessIdAsync(string businessId, CancellationToken ct = default);
    Task<InstagramAccount?> GetByPageIdAsync(string pageId, CancellationToken ct = default);
    Task<IEnumerable<InstagramAccount>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<Guid> CreateOrUpdateAsync(InstagramAccount account, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, Guid tenantId, Domain.Enums.InstagramAccountStatus status, CancellationToken ct = default);
    Task UpdateWebhookSubscribedAsync(Guid id, bool subscribed, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
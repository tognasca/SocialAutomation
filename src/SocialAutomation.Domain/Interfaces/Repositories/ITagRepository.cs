
using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface ITagRepository
{
    Task<IEnumerable<Tag>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<Tag?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Tag?> GetByNameAsync(string name, Guid tenantId, CancellationToken ct = default);
    Task<Guid> CreateAsync(Tag tag, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task AssignTagToLeadAsync(Guid leadId, Guid tagId, CancellationToken ct = default);
    Task RemoveTagFromLeadAsync(Guid leadId, Guid tagId, CancellationToken ct = default);
    Task<IEnumerable<Tag>> GetTagsByLeadIdAsync(Guid leadId, CancellationToken ct = default);
}
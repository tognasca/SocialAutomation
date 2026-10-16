
using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface ICrmTaskRepository
{
    Task<CrmTask?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<CrmTask>> GetByLeadIdAsync(Guid leadId, Guid tenantId, CancellationToken ct = default);
    Task<(IEnumerable<CrmTask> Items, int Total)> GetPagedAsync(
        Guid tenantId, int page, int pageSize, string? status = null, Guid? assignedUserId = null, CancellationToken ct = default);
    Task<Guid> CreateAsync(CrmTask task, CancellationToken ct = default);
    Task UpdateAsync(CrmTask task, CancellationToken ct = default);
    Task CompleteTaskAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
}
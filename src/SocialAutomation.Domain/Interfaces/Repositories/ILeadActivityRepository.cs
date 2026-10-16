
using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface ILeadActivityRepository
{
    Task<Guid> CreateAsync(LeadActivity activity, CancellationToken ct = default);
    Task<IEnumerable<LeadActivity>> GetByLeadIdAsync(Guid leadId, Guid tenantId, CancellationToken ct = default);
}
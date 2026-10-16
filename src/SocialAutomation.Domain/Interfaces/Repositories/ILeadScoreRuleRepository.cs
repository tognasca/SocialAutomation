
using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface ILeadScoreRuleRepository
{
    Task<IEnumerable<LeadScoreRule>> GetRulesByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<int> GetPointsForTriggerAsync(Guid tenantId, string trigger, CancellationToken ct = default);
    Task SaveRulesAsync(Guid tenantId, IEnumerable<LeadScoreRule> rules, CancellationToken ct = default);
}
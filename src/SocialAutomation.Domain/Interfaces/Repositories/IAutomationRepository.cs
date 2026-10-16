using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IAutomationRepository
{
    Task<IEnumerable<AutomationFlow>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<AutomationFlow>> GetActiveByTriggerAsync(Guid tenantId, string triggerType, CancellationToken ct = default);
    Task<AutomationFlow?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Guid> CreateFlowAsync(AutomationFlow flow, CancellationToken ct = default);
    Task UpdateFlowAsync(AutomationFlow flow, CancellationToken ct = default);
    Task ToggleActiveAsync(Guid id, Guid tenantId, bool isActive, CancellationToken ct = default);
    Task DeleteFlowAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    // Nodes
    Task SaveNodesAsync(Guid flowId, Guid tenantId, IEnumerable<AutomationNode> nodes, CancellationToken ct = default);
    Task<IEnumerable<AutomationNode>> GetNodesByFlowIdAsync(Guid flowId, CancellationToken ct = default);

    // Executions
    Task<Guid> CreateExecutionAsync(AutomationExecution execution, CancellationToken ct = default);
    Task UpdateExecutionAsync(AutomationExecution execution, CancellationToken ct = default);
    Task<IEnumerable<AutomationExecution>> GetPendingDelayedExecutionsAsync(int limit = 50, CancellationToken ct = default);
}

public interface ICampaignRepository
{
    Task<IEnumerable<Campaign>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<Campaign?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Guid> CreateAsync(Campaign campaign, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, Guid tenantId, string status, CancellationToken ct = default);
    Task CreateRecipientsAsync(IEnumerable<CampaignRecipient> recipients, CancellationToken ct = default);
    Task<IEnumerable<CampaignRecipient>> GetPendingRecipientsAsync(Guid campaignId, int limit = 50, CancellationToken ct = default);
    Task UpdateRecipientStatusAsync(Guid recipientId, string status, string? error = null, CancellationToken ct = default);
}
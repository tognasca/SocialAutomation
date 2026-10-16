
using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface ILeadRepository
{
    Task<Lead?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Lead?> GetByInstagramUserIdAsync(string instagramUserId, Guid tenantId, CancellationToken ct = default);
    Task<Lead?> GetByInstagramUsernameAsync(string username, Guid tenantId, CancellationToken ct = default);
    Task<(IEnumerable<Lead> Items, int Total)> GetPagedAsync(
        Guid tenantId, int page, int pageSize, string? search = null, string? status = null,
        string? pipelineStage = null, Guid? tagId = null, Guid? ownerUserId = null, CancellationToken ct = default);
    Task<IEnumerable<Lead>> GetByPipelineStageAsync(Guid tenantId, string stage, CancellationToken ct = default);
    Task<Guid> CreateAsync(Lead lead, CancellationToken ct = default);
    Task UpdateAsync(Lead lead, CancellationToken ct = default);
    Task UpdatePipelineStageAsync(Guid id, Guid tenantId, string stage, int order, CancellationToken ct = default);
    Task UpdateScoreAsync(Guid id, Guid tenantId, int score, string classification, CancellationToken ct = default);
    Task UpdateNextFollowUpAsync(Guid id, Guid tenantId, DateTime? nextFollowUp, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Dictionary<string, int>> GetCountByStagesAsync(Guid tenantId, CancellationToken ct = default);
}
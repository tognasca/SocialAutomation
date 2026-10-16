using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IAiAgentRepository
{
    Task<IEnumerable<AiAgent>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<AiAgent?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<AiAgent?> GetDefaultAsync(Guid tenantId, CancellationToken ct = default);
    Task<Guid> CreateAsync(AiAgent agent, CancellationToken ct = default);
    Task UpdateAsync(AiAgent agent, CancellationToken ct = default);
    Task SetDefaultAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
}

public interface IKnowledgeRepository
{
    Task<IEnumerable<KnowledgeBase>> GetBasesAsync(Guid tenantId, CancellationToken ct = default);
    Task<Guid> CreateBaseAsync(KnowledgeBase kb, CancellationToken ct = default);
    Task<IEnumerable<KnowledgeDocument>> GetDocumentsAsync(Guid tenantId, Guid? knowledgeBaseId = null, CancellationToken ct = default);
    Task<Guid> CreateDocumentAsync(KnowledgeDocument doc, CancellationToken ct = default);
    Task DeleteDocumentAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task CreateChunksAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken ct = default);
    Task DeleteChunksByDocumentAsync(Guid documentId, Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<KnowledgeChunk>> SearchChunksAsync(Guid tenantId, string query, int limit = 5, CancellationToken ct = default);
}

public interface IAiMessageLogRepository
{
    Task CreateAsync(AiMessageLog log, CancellationToken ct = default);
}
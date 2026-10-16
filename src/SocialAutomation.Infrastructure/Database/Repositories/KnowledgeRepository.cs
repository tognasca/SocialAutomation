using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class KnowledgeRepository : IKnowledgeRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public KnowledgeRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<KnowledgeBase>> GetBasesAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<KnowledgeBase>(
            @"SELECT id, tenant_id AS TenantId, name, description, is_active AS IsActive,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM knowledge_bases WHERE tenant_id = @TenantId ORDER BY name",
            new { TenantId = tenantId });
    }

    public async Task<Guid> CreateBaseAsync(KnowledgeBase kb, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO knowledge_bases (id, tenant_id, name, description, is_active, created_at, updated_at)
              VALUES (@Id, @TenantId, @Name, @Description, @IsActive, @CreatedAt, @UpdatedAt)", kb);
        return kb.Id;
    }

    public async Task<IEnumerable<KnowledgeDocument>> GetDocumentsAsync(Guid tenantId, Guid? knowledgeBaseId = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id, tenant_id AS TenantId, knowledge_base_id AS KnowledgeBaseId, title, content,
                   source_type AS SourceType, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM knowledge_documents
            WHERE tenant_id = @TenantId";
        if (knowledgeBaseId.HasValue) sql += " AND knowledge_base_id = @KnowledgeBaseId";
        sql += " ORDER BY created_at DESC";

        return await connection.QueryAsync<KnowledgeDocument>(sql, new { TenantId = tenantId, KnowledgeBaseId = knowledgeBaseId });
    }

    public async Task<Guid> CreateDocumentAsync(KnowledgeDocument doc, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO knowledge_documents (id, tenant_id, knowledge_base_id, title, content, source_type, is_active, created_at, updated_at)
              VALUES (@Id, @TenantId, @KnowledgeBaseId, @Title, @Content, @SourceType, @IsActive, @CreatedAt, @UpdatedAt)", doc);
        return doc.Id;
    }

    public async Task DeleteDocumentAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM knowledge_documents WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task CreateChunksAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        foreach (var chunk in chunks)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO knowledge_chunks (id, tenant_id, document_id, chunk_index, content, token_count, embedding_json, created_at)
                  VALUES (@Id, @TenantId, @DocumentId, @ChunkIndex, @Content, @TokenCount, @EmbeddingJson, @CreatedAt)", chunk);
        }
    }

    public async Task DeleteChunksByDocumentAsync(Guid documentId, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM knowledge_chunks WHERE document_id = @DocumentId AND tenant_id = @TenantId",
            new { DocumentId = documentId, TenantId = tenantId });
    }

    public async Task<IEnumerable<KnowledgeChunk>> SearchChunksAsync(Guid tenantId, string query, int limit = 5, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        // Busca textual inicial (RAG vetorial virá depois)
        return await connection.QueryAsync<KnowledgeChunk>(
            @"SELECT id, tenant_id AS TenantId, document_id AS DocumentId, chunk_index AS ChunkIndex,
                     content, token_count AS TokenCount, embedding_json AS EmbeddingJson, created_at AS CreatedAt
              FROM knowledge_chunks
              WHERE tenant_id = @TenantId
                AND content ILIKE @Query
              ORDER BY created_at DESC
              LIMIT @Limit",
            new { TenantId = tenantId, Query = $"%{query}%", Limit = limit });
    }
}
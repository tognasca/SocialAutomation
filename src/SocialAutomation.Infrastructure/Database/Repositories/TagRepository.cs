
using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class TagRepository : ITagRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public TagRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Tag>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Tag>(
            @"SELECT id, tenant_id AS TenantId, name, color, created_at AS CreatedAt
              FROM tags WHERE tenant_id = @TenantId ORDER BY name",
            new { TenantId = tenantId });
    }

    public async Task<Tag?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Tag>(
            @"SELECT id, tenant_id AS TenantId, name, color, created_at AS CreatedAt
              FROM tags WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task<Tag?> GetByNameAsync(string name, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Tag>(
            @"SELECT id, tenant_id AS TenantId, name, color, created_at AS CreatedAt
              FROM tags WHERE LOWER(name) = LOWER(@Name) AND tenant_id = @TenantId",
            new { Name = name.Trim(), TenantId = tenantId });
    }

    public async Task<Guid> CreateAsync(Tag tag, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO tags (id, tenant_id, name, color, created_at)
              VALUES (@Id, @TenantId, @Name, @Color, @CreatedAt)
              ON CONFLICT (tenant_id, name) DO NOTHING",
            tag);
        return tag.Id;
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM tags WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task AssignTagToLeadAsync(Guid leadId, Guid tagId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO lead_tags (id, lead_id, tag_id, created_at)
              VALUES (@Id, @LeadId, @TagId, NOW())
              ON CONFLICT (lead_id, tag_id) DO NOTHING",
            new { Id = Guid.NewGuid(), LeadId = leadId, TagId = tagId });
    }

    public async Task RemoveTagFromLeadAsync(Guid leadId, Guid tagId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM lead_tags WHERE lead_id = @LeadId AND tag_id = @TagId",
            new { LeadId = leadId, TagId = tagId });
    }

    public async Task<IEnumerable<Tag>> GetTagsByLeadIdAsync(Guid leadId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Tag>(
            @"SELECT t.id, t.tenant_id AS TenantId, t.name, t.color, t.created_at AS CreatedAt
              FROM tags t
              INNER JOIN lead_tags lt ON lt.tag_id = t.id
              WHERE lt.lead_id = @LeadId",
            new { LeadId = leadId });
    }
}
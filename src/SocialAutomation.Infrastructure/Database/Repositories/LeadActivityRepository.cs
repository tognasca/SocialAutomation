
using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class LeadActivityRepository : ILeadActivityRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public LeadActivityRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> CreateAsync(LeadActivity activity, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO lead_activities (id, tenant_id, lead_id, user_id, type, title, description, metadata, created_at)
              VALUES (@Id, @TenantId, @LeadId, @UserId, @Type, @Title, @Description, @Metadata::jsonb, @CreatedAt)",
            new
            {
                activity.Id,
                activity.TenantId,
                activity.LeadId,
                activity.UserId,
                activity.Type,
                activity.Title,
                activity.Description,
                Metadata = activity.Metadata ?? "{}",
                activity.CreatedAt
            });

        return activity.Id;
    }

    public async Task<IEnumerable<LeadActivity>> GetByLeadIdAsync(Guid leadId, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT la.id, la.tenant_id AS TenantId, la.lead_id AS LeadId, la.user_id AS UserId,
                   la.type, la.title, la.description, la.metadata, la.created_at AS CreatedAt,
                   u.name AS UserName
            FROM lead_activities la
            LEFT JOIN users u ON u.id = la.user_id
            WHERE la.lead_id = @LeadId AND la.tenant_id = @TenantId
            ORDER BY la.created_at DESC";

        return await connection.QueryAsync<LeadActivity>(sql, new { LeadId = leadId, TenantId = tenantId });
    }
}
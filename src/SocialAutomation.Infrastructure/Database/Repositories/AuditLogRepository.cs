using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public AuditLogRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task CreateAsync(AuditLog auditLog, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO audit_logs (id, tenant_id, user_id, action, entity_type, entity_id,
                                       old_values, new_values, ip_address, user_agent, correlation_id, created_at)
              VALUES (@Id, @TenantId, @UserId, @Action, @EntityType, @EntityId,
                      @OldValues::jsonb, @NewValues::jsonb, @IpAddress, @UserAgent, @CorrelationId, @CreatedAt)",
            new
            {
                auditLog.Id,
                auditLog.TenantId,
                auditLog.UserId,
                auditLog.Action,
                auditLog.EntityType,
                auditLog.EntityId,
                auditLog.OldValues,
                auditLog.NewValues,
                auditLog.IpAddress,
                auditLog.UserAgent,
                auditLog.CorrelationId,
                auditLog.CreatedAt
            });
    }

    public async Task<(IEnumerable<AuditLog> Items, int Total)> GetPagedAsync(
        Guid tenantId, int page, int pageSize, string? action = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var whereClause = "WHERE tenant_id = @TenantId";
        if (!string.IsNullOrWhiteSpace(action))
        {
            whereClause += " AND action = @Action";
        }

        var total = await connection.QueryFirstOrDefaultAsync<int>(
            $"SELECT COUNT(*) FROM audit_logs {whereClause}",
            new { TenantId = tenantId, Action = action });

        var items = await connection.QueryAsync<AuditLog>(
            $@"SELECT id, tenant_id AS TenantId, user_id AS UserId, action,
                      entity_type AS EntityType, entity_id AS EntityId,
                      old_values AS OldValues, new_values AS NewValues,
                      ip_address AS IpAddress, user_agent AS UserAgent,
                      correlation_id AS CorrelationId, created_at AS CreatedAt
               FROM audit_logs {whereClause}
               ORDER BY created_at DESC
               LIMIT @PageSize OFFSET @Offset",
            new
            {
                TenantId = tenantId,
                Action = action,
                PageSize = pageSize,
                Offset = (page - 1) * pageSize
            });

        return (items, total);
    }
}
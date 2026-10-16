
using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class CrmTaskRepository : ICrmTaskRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public CrmTaskRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CrmTask?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT t.id, t.tenant_id AS TenantId, t.lead_id AS LeadId, t.assigned_user_id AS AssignedUserId,
                   t.title, t.description, t.due_date AS DueDate, t.status, t.priority,
                   t.completed_at AS CompletedAt, t.created_at AS CreatedAt, t.updated_at AS UpdatedAt,
                   l.name AS LeadName, u.name AS AssignedUserName
            FROM crm_tasks t
            INNER JOIN leads l ON l.id = t.lead_id
            LEFT JOIN users u ON u.id = t.assigned_user_id
            WHERE t.id = @Id AND t.tenant_id = @TenantId";

        return await connection.QueryFirstOrDefaultAsync<CrmTask>(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<IEnumerable<CrmTask>> GetByLeadIdAsync(Guid leadId, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT t.id, t.tenant_id AS TenantId, t.lead_id AS LeadId, t.assigned_user_id AS AssignedUserId,
                   t.title, t.description, t.due_date AS DueDate, t.status, t.priority,
                   t.completed_at AS CompletedAt, t.created_at AS CreatedAt, t.updated_at AS UpdatedAt,
                   u.name AS AssignedUserName
            FROM crm_tasks t
            LEFT JOIN users u ON u.id = t.assigned_user_id
            WHERE t.lead_id = @LeadId AND t.tenant_id = @TenantId
            ORDER BY t.due_date ASC";

        return await connection.QueryAsync<CrmTask>(sql, new { LeadId = leadId, TenantId = tenantId });
    }

    public async Task<(IEnumerable<CrmTask> Items, int Total)> GetPagedAsync(
        Guid tenantId, int page, int pageSize, string? status = null, Guid? assignedUserId = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var where = "WHERE t.tenant_id = @TenantId";
        if (!string.IsNullOrWhiteSpace(status))
            where += " AND t.status = @Status";
        if (assignedUserId.HasValue)
            where += " AND t.assigned_user_id = @AssignedUserId";

        var total = await connection.QueryFirstOrDefaultAsync<int>(
            $"SELECT COUNT(*) FROM crm_tasks t {where}",
            new { TenantId = tenantId, Status = status, AssignedUserId = assignedUserId });

        var sql = $@"
            SELECT t.id, t.tenant_id AS TenantId, t.lead_id AS LeadId, t.assigned_user_id AS AssignedUserId,
                   t.title, t.description, t.due_date AS DueDate, t.status, t.priority,
                   t.completed_at AS CompletedAt, t.created_at AS CreatedAt,
                   l.name AS LeadName, u.name AS AssignedUserName
            FROM crm_tasks t
            INNER JOIN leads l ON l.id = t.lead_id
            LEFT JOIN users u ON u.id = t.assigned_user_id
            {where}
            ORDER BY t.due_date ASC
            LIMIT @PageSize OFFSET @Offset";

        var items = await connection.QueryAsync<CrmTask>(sql, new
        {
            TenantId = tenantId,
            Status = status,
            AssignedUserId = assignedUserId,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        });

        return (items, total);
    }

    public async Task<Guid> CreateAsync(CrmTask task, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO crm_tasks (id, tenant_id, lead_id, assigned_user_id, title, description, due_date, status, priority, created_at, updated_at)
            VALUES (@Id, @TenantId, @LeadId, @AssignedUserId, @Title, @Description, @DueDate, @Status, @Priority, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(sql, task);
        return task.Id;
    }

    public async Task UpdateAsync(CrmTask task, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE crm_tasks
            SET title = @Title, description = @Description, due_date = @DueDate,
                status = @Status, priority = @Priority, assigned_user_id = @AssignedUserId, updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId";

        await connection.ExecuteAsync(sql, task);
    }

    public async Task CompleteTaskAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE crm_tasks
              SET status = 'Concluida', completed_at = NOW(), updated_at = NOW()
              WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM crm_tasks WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }
}
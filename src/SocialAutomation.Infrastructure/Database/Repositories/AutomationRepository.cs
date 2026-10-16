using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class AutomationRepository : IAutomationRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public AutomationRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<AutomationFlow>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<AutomationFlow>(
            @"SELECT id, tenant_id AS TenantId, name, description, trigger_type AS TriggerType,
                     trigger_config AS TriggerConfig, is_active AS IsActive, execution_count AS ExecutionCount,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM automation_flows WHERE tenant_id = @TenantId ORDER BY created_at DESC",
            new { TenantId = tenantId });
    }

    public async Task<IEnumerable<AutomationFlow>> GetActiveByTriggerAsync(Guid tenantId, string triggerType, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var flows = (await connection.QueryAsync<AutomationFlow>(
            @"SELECT id, tenant_id AS TenantId, name, description, trigger_type AS TriggerType,
                     trigger_config AS TriggerConfig, is_active AS IsActive, execution_count AS ExecutionCount
              FROM automation_flows
              WHERE tenant_id = @TenantId AND trigger_type = @TriggerType AND is_active = TRUE",
            new { TenantId = tenantId, TriggerType = triggerType })).ToList();

        foreach (var flow in flows)
        {
            flow.Nodes = (await GetNodesByFlowIdAsync(flow.Id, ct)).ToList();
        }

        return flows;
    }

    public async Task<AutomationFlow?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var flow = await connection.QueryFirstOrDefaultAsync<AutomationFlow>(
            @"SELECT id, tenant_id AS TenantId, name, description, trigger_type AS TriggerType,
                     trigger_config AS TriggerConfig, is_active AS IsActive, execution_count AS ExecutionCount,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM automation_flows WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });

        if (flow != null)
        {
            flow.Nodes = (await GetNodesByFlowIdAsync(flow.Id, ct)).ToList();
        }

        return flow;
    }

    public async Task<Guid> CreateFlowAsync(AutomationFlow flow, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO automation_flows (id, tenant_id, name, description, trigger_type, trigger_config, is_active, created_at, updated_at)
              VALUES (@Id, @TenantId, @Name, @Description, @TriggerType, @TriggerConfig::jsonb, @IsActive, @CreatedAt, @UpdatedAt)",
            new
            {
                flow.Id,
                flow.TenantId,
                flow.Name,
                flow.Description,
                flow.TriggerType,
                TriggerConfig = flow.TriggerConfig ?? "{}",
                flow.IsActive,
                flow.CreatedAt,
                flow.UpdatedAt
            });

        return flow.Id;
    }

    public async Task UpdateFlowAsync(AutomationFlow flow, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE automation_flows
              SET name = @Name, description = @Description, trigger_type = @TriggerType,
                  trigger_config = @TriggerConfig::jsonb, is_active = @IsActive, updated_at = NOW()
              WHERE id = @Id AND tenant_id = @TenantId",
            new
            {
                flow.Id,
                flow.TenantId,
                flow.Name,
                flow.Description,
                flow.TriggerType,
                TriggerConfig = flow.TriggerConfig ?? "{}",
                flow.IsActive
            });
    }

    public async Task ToggleActiveAsync(Guid id, Guid tenantId, bool isActive, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE automation_flows SET is_active = @IsActive, updated_at = NOW() WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId, IsActive = isActive });
    }

    public async Task DeleteFlowAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM automation_flows WHERE id = @Id AND tenant_id = @TenantId", new { Id = id, TenantId = tenantId });
    }

    public async Task SaveNodesAsync(Guid flowId, Guid tenantId, IEnumerable<AutomationNode> nodes, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM automation_nodes WHERE flow_id = @FlowId AND tenant_id = @TenantId", new { FlowId = flowId, TenantId = tenantId });

        var sql = @"
            INSERT INTO automation_nodes (id, tenant_id, flow_id, step_order, node_type, action_or_condition, config, created_at)
            VALUES (@Id, @TenantId, @FlowId, @StepOrder, @NodeType, @ActionOrCondition, @Config::jsonb, NOW())";

        foreach (var node in nodes)
        {
            await connection.ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FlowId = flowId,
                node.StepOrder,
                node.NodeType,
                node.ActionOrCondition,
                Config = node.Config ?? "{}"
            });
        }
    }

    public async Task<IEnumerable<AutomationNode>> GetNodesByFlowIdAsync(Guid flowId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<AutomationNode>(
            @"SELECT id, tenant_id AS TenantId, flow_id AS FlowId, step_order AS StepOrder,
                     node_type AS NodeType, action_or_condition AS ActionOrCondition, config
              FROM automation_nodes WHERE flow_id = @FlowId ORDER BY step_order ASC",
            new { FlowId = flowId });
    }

    public async Task<Guid> CreateExecutionAsync(AutomationExecution execution, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO automation_executions (id, tenant_id, flow_id, lead_id, current_step, status, next_execution_at, execution_log, created_at, updated_at)
              VALUES (@Id, @TenantId, @FlowId, @LeadId, @CurrentStep, @Status, @NextExecutionAt, @ExecutionLog::jsonb, @CreatedAt, @UpdatedAt)",
            new
            {
                execution.Id,
                execution.TenantId,
                execution.FlowId,
                execution.LeadId,
                execution.CurrentStep,
                execution.Status,
                execution.NextExecutionAt,
                ExecutionLog = execution.ExecutionLog ?? "[]",
                execution.CreatedAt,
                execution.UpdatedAt
            });

        return execution.Id;
    }

    public async Task UpdateExecutionAsync(AutomationExecution execution, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE automation_executions
              SET current_step = @CurrentStep, status = @Status, next_execution_at = @NextExecutionAt,
                  execution_log = @ExecutionLog::jsonb, updated_at = NOW()
              WHERE id = @Id AND tenant_id = @TenantId",
            new
            {
                execution.Id,
                execution.TenantId,
                execution.CurrentStep,
                execution.Status,
                execution.NextExecutionAt,
                ExecutionLog = execution.ExecutionLog ?? "[]"
            });
    }

    public async Task<IEnumerable<AutomationExecution>> GetPendingDelayedExecutionsAsync(int limit = 50, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<AutomationExecution>(
            @"SELECT id, tenant_id AS TenantId, flow_id AS FlowId, lead_id AS LeadId,
                     current_step AS CurrentStep, status, next_execution_at AS NextExecutionAt, execution_log AS ExecutionLog
              FROM automation_executions
              WHERE status = 'WaitingDelay' AND next_execution_at <= NOW()
              LIMIT @Limit",
            new { Limit = limit });
    }
}
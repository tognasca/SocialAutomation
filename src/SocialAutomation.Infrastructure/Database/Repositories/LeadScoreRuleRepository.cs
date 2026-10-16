
using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class LeadScoreRuleRepository : ILeadScoreRuleRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public LeadScoreRuleRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<LeadScoreRule>> GetRulesByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<LeadScoreRule>(
            @"SELECT id, tenant_id AS TenantId, event_trigger AS EventTrigger, points, is_active AS IsActive, created_at AS CreatedAt
              FROM lead_score_rules WHERE tenant_id = @TenantId",
            new { TenantId = tenantId });
    }

    public async Task<int> GetPointsForTriggerAsync(Guid tenantId, string trigger, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<int>(
            @"SELECT points FROM lead_score_rules
              WHERE tenant_id = @TenantId AND event_trigger = @Trigger AND is_active = TRUE",
            new { TenantId = tenantId, Trigger = trigger });
    }

    public async Task SaveRulesAsync(Guid tenantId, IEnumerable<LeadScoreRule> rules, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM lead_score_rules WHERE tenant_id = @TenantId",
            new { TenantId = tenantId });

        var sql = @"
            INSERT INTO lead_score_rules (id, tenant_id, event_trigger, points, is_active, created_at)
            VALUES (@Id, @TenantId, @EventTrigger, @Points, @IsActive, NOW())";

        foreach (var rule in rules)
        {
            await connection.ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                rule.EventTrigger,
                rule.Points,
                rule.IsActive
            });
        }
    }
}
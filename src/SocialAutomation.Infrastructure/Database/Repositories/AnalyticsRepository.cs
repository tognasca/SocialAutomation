using Dapper;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Models.Analytics;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public AnalyticsRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT
                (SELECT COUNT(*) FROM leads
                 WHERE tenant_id = @TenantId AND created_at >= @From AND created_at < @To) AS NewLeads,

                (SELECT COUNT(*) FROM conversations
                 WHERE tenant_id = @TenantId AND created_at >= @From AND created_at < @To) AS Conversations,

                (SELECT COUNT(*) FROM leads
                 WHERE tenant_id = @TenantId
                   AND score_classification IN ('Quente', 'Alta Intenção')
                   AND created_at >= @From AND created_at < @To) AS HotLeads,

                (SELECT COUNT(*) FROM leads
                 WHERE tenant_id = @TenantId
                   AND pipeline_stage IN ('Proposta', 'Negociação')
                   AND updated_at >= @From AND updated_at < @To) AS Opportunities,

                (SELECT COUNT(*) FROM leads
                 WHERE tenant_id = @TenantId
                   AND pipeline_stage = 'Cliente'
                   AND updated_at >= @From AND updated_at < @To) AS Clients,

                (SELECT COUNT(*) FROM crm_tasks
                 WHERE tenant_id = @TenantId
                   AND status = 'Pendente'
                   AND due_date >= @From AND due_date < @To) AS FollowUpsPending,

                (SELECT COUNT(*) FROM messages
                 WHERE tenant_id = @TenantId
                   AND sender_type = 'Lead'
                   AND created_at >= @From AND created_at < @To) AS MessagesReceived,

                (SELECT COUNT(*) FROM messages
                 WHERE tenant_id = @TenantId
                   AND sender_type IN ('User', 'AI', 'System')
                   AND created_at >= @From AND created_at < @To) AS MessagesSent,

                (SELECT COUNT(*) FROM messages
                 WHERE tenant_id = @TenantId
                   AND sender_type = 'AI'
                   AND created_at >= @From AND created_at < @To) AS AiReplies,

                (SELECT COUNT(*) FROM messages
                 WHERE tenant_id = @TenantId
                   AND sender_type = 'System'
                   AND content ILIKE '%Transferido para atendimento humano%'
                   AND created_at >= @From AND created_at < @To) AS HumanHandoffs,

                (SELECT COALESCE(SUM(sent_count), 0) FROM campaigns
                 WHERE tenant_id = @TenantId
                   AND updated_at >= @From AND updated_at < @To) AS CampaignsSent,

                (SELECT COUNT(*) FROM content_items
                 WHERE tenant_id = @TenantId
                   AND status = 'Publicado'
                   AND COALESCE(published_at, updated_at) >= @From
                   AND COALESCE(published_at, updated_at) < @To) AS ContentPublished,

                (SELECT COALESCE(SUM(deal_value), 0) FROM leads
                 WHERE tenant_id = @TenantId
                   AND pipeline_stage NOT IN ('Perdido', 'Sem Interesse')) AS TotalPipelineValue
        ";

        var summary = await connection.QueryFirstAsync<DashboardSummary>(sql, new
        {
            TenantId = tenantId,
            From = from,
            To = to
        });

        if (summary.NewLeads > 0)
        {
            summary.ConversionRate = Math.Round((decimal)summary.Clients * 100m / summary.NewLeads, 2);
        }

        return summary;
    }

    public async Task<IEnumerable<FunnelStageMetric>> GetFunnelMetricsAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<FunnelStageMetric>(
            @"SELECT pipeline_stage AS Stage,
                     COUNT(*) AS Count,
                     COALESCE(SUM(deal_value), 0) AS TotalValue
              FROM leads
              WHERE tenant_id = @TenantId
              GROUP BY pipeline_stage
              ORDER BY
                CASE pipeline_stage
                  WHEN 'Novo' THEN 1
                  WHEN 'Contato Iniciado' THEN 2
                  WHEN 'Respondeu' THEN 3
                  WHEN 'Interessado' THEN 4
                  WHEN 'Proposta' THEN 5
                  WHEN 'Negociação' THEN 6
                  WHEN 'Cliente' THEN 7
                  ELSE 99
                END",
            new { TenantId = tenantId });
    }

    public async Task<IEnumerable<TimeSeriesPoint>> GetTimeSeriesAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            WITH days AS (
                SELECT generate_series(@From::date, (@To::date - INTERVAL '1 day')::date, INTERVAL '1 day')::date AS day
            )
            SELECT
                d.day AS Date,
                COALESCE(l.cnt, 0) AS Leads,
                COALESCE(c.cnt, 0) AS Conversations,
                COALESCE(m.cnt, 0) AS Messages,
                COALESCE(w.cnt, 0) AS Clients
            FROM days d
            LEFT JOIN (
                SELECT created_at::date AS day, COUNT(*) AS cnt
                FROM leads
                WHERE tenant_id = @TenantId AND created_at >= @From AND created_at < @To
                GROUP BY created_at::date
            ) l ON l.day = d.day
            LEFT JOIN (
                SELECT created_at::date AS day, COUNT(*) AS cnt
                FROM conversations
                WHERE tenant_id = @TenantId AND created_at >= @From AND created_at < @To
                GROUP BY created_at::date
            ) c ON c.day = d.day
            LEFT JOIN (
                SELECT created_at::date AS day, COUNT(*) AS cnt
                FROM messages
                WHERE tenant_id = @TenantId AND created_at >= @From AND created_at < @To
                GROUP BY created_at::date
            ) m ON m.day = d.day
            LEFT JOIN (
                SELECT updated_at::date AS day, COUNT(*) AS cnt
                FROM leads
                WHERE tenant_id = @TenantId
                  AND pipeline_stage = 'Cliente'
                  AND updated_at >= @From AND updated_at < @To
                GROUP BY updated_at::date
            ) w ON w.day = d.day
            ORDER BY d.day";

        return await connection.QueryAsync<TimeSeriesPoint>(sql, new
        {
            TenantId = tenantId,
            From = from.Date,
            To = to.Date
        });
    }

    public async Task<IEnumerable<SourceMetric>> GetLeadSourcesAsync(
        Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<SourceMetric>(
            @"SELECT COALESCE(source, 'desconhecido') AS Source, COUNT(*) AS Count
              FROM leads
              WHERE tenant_id = @TenantId
                AND created_at >= @From AND created_at < @To
              GROUP BY COALESCE(source, 'desconhecido')
              ORDER BY Count DESC",
            new { TenantId = tenantId, From = from, To = to });
    }

    public async Task<IEnumerable<ScoreDistributionMetric>> GetScoreDistributionAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ScoreDistributionMetric>(
            @"SELECT COALESCE(score_classification, 'Frio') AS Classification, COUNT(*) AS Count
              FROM leads
              WHERE tenant_id = @TenantId
              GROUP BY COALESCE(score_classification, 'Frio')
              ORDER BY
                CASE COALESCE(score_classification, 'Frio')
                  WHEN 'Frio' THEN 1
                  WHEN 'Morno' THEN 2
                  WHEN 'Quente' THEN 3
                  WHEN 'Alta Intenção' THEN 4
                  ELSE 5
                END",
            new { TenantId = tenantId });
    }
}
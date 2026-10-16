
using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class CampaignRepository : ICampaignRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public CampaignRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Campaign>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Campaign>(
            @"SELECT id, tenant_id AS TenantId, instagram_account_id AS InstagramAccountId, name,
                     message_template AS MessageTemplate, segment_filter AS SegmentFilter, status,
                     scheduled_at AS ScheduledAt, total_audience AS TotalAudience, sent_count AS SentCount,
                     failed_count AS FailedCount, created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM campaigns WHERE tenant_id = @TenantId ORDER BY created_at DESC",
            new { TenantId = tenantId });
    }

    public async Task<Campaign?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Campaign>(
            @"SELECT id, tenant_id AS TenantId, instagram_account_id AS InstagramAccountId, name,
                     message_template AS MessageTemplate, segment_filter AS SegmentFilter, status,
                     scheduled_at AS ScheduledAt, total_audience AS TotalAudience, sent_count AS SentCount,
                     failed_count AS FailedCount, created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM campaigns WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task<Guid> CreateAsync(Campaign campaign, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO campaigns (id, tenant_id, instagram_account_id, name, message_template, segment_filter, status, scheduled_at, total_audience, created_at, updated_at)
              VALUES (@Id, @TenantId, @InstagramAccountId, @Name, @MessageTemplate, @SegmentFilter::jsonb, @Status, @ScheduledAt, @TotalAudience, @CreatedAt, @UpdatedAt)",
            new
            {
                campaign.Id,
                campaign.TenantId,
                campaign.InstagramAccountId,
                campaign.Name,
                campaign.MessageTemplate,
                SegmentFilter = campaign.SegmentFilter ?? "{}",
                campaign.Status,
                campaign.ScheduledAt,
                campaign.TotalAudience,
                campaign.CreatedAt,
                campaign.UpdatedAt
            });

        return campaign.Id;
    }

    public async Task UpdateStatusAsync(Guid id, Guid tenantId, string status, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE campaigns SET status = @Status, updated_at = NOW() WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId, Status = status });
    }

    public async Task CreateRecipientsAsync(IEnumerable<CampaignRecipient> recipients, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO campaign_recipients (id, tenant_id, campaign_id, lead_id, status, created_at)
            VALUES (@Id, @TenantId, @CampaignId, @LeadId, @Status, NOW())";

        foreach (var r in recipients)
        {
            await connection.ExecuteAsync(sql, r);
        }
    }

    public async Task<IEnumerable<CampaignRecipient>> GetPendingRecipientsAsync(Guid campaignId, int limit = 50, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<CampaignRecipient>(
            @"SELECT id, tenant_id AS TenantId, campaign_id AS CampaignId, lead_id AS LeadId, status
              FROM campaign_recipients WHERE campaign_id = @CampaignId AND status = 'Pendente' LIMIT @Limit",
            new { CampaignId = campaignId, Limit = limit });
    }

    public async Task UpdateRecipientStatusAsync(Guid recipientId, string status, string? error = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE campaign_recipients
              SET status = @Status, error_message = @Error, sent_at = CASE WHEN @Status = 'Enviado' THEN NOW() ELSE sent_at END
              WHERE id = @Id",
            new { Id = recipientId, Status = status, Error = error });
    }
}
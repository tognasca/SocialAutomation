
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
                     instagram_media_id AS InstagramMediaId, comment_keyword AS CommentKeyword,
                     message_template AS MessageTemplate, segment_filter AS SegmentFilter, status,
                     scheduled_at AS ScheduledAt, total_audience AS TotalAudience, sent_count AS SentCount,
                     failed_count AS FailedCount,
                     (SELECT count(*)::int FROM instagram_comment_replies WHERE campaign_id = campaigns.id AND replied_at IS NOT NULL) AS ReplyCount,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM campaigns WHERE tenant_id = @TenantId ORDER BY created_at DESC",
            new { TenantId = tenantId });
    }

    public async Task<Campaign?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Campaign>(
            @"SELECT id, tenant_id AS TenantId, instagram_account_id AS InstagramAccountId, name,
                     instagram_media_id AS InstagramMediaId, comment_keyword AS CommentKeyword,
                     message_template AS MessageTemplate, segment_filter AS SegmentFilter, status,
                     scheduled_at AS ScheduledAt, total_audience AS TotalAudience, sent_count AS SentCount,
                     failed_count AS FailedCount,
                     (SELECT count(*)::int FROM instagram_comment_replies WHERE campaign_id = campaigns.id AND replied_at IS NOT NULL) AS ReplyCount,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM campaigns WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task<Guid> CreateAsync(Campaign campaign, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO campaigns (id, tenant_id, instagram_account_id, name, message_template, segment_filter, status, scheduled_at, total_audience, created_at, updated_at, instagram_media_id, comment_keyword)
              VALUES (@Id, @TenantId, @InstagramAccountId, @Name, @MessageTemplate, @SegmentFilter::jsonb, @Status, @ScheduledAt, @TotalAudience, @CreatedAt, @UpdatedAt, @InstagramMediaId, @CommentKeyword)",
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
                campaign.UpdatedAt,
                campaign.InstagramMediaId,
                campaign.CommentKeyword
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

    public async Task<IEnumerable<Campaign>> GetActiveCommentCampaignsAsync(Guid tenantId, Guid accountId, string mediaId, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Campaign>(new CommandDefinition(
            """
            SELECT id, instagram_account_id AS InstagramAccountId, comment_keyword AS CommentKeyword,
                   message_template AS MessageTemplate
            FROM campaigns
            WHERE tenant_id = @TenantId AND instagram_account_id = @AccountId
              AND instagram_media_id = @MediaId AND status = 'Ativa'
            ORDER BY char_length(comment_keyword) DESC, created_at, id
            """, new { tenantId, accountId, mediaId }, cancellationToken: ct));
    }

    public async Task<bool> TryClaimCommentAsync(Campaign campaign, string commentId, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO instagram_comment_replies (instagram_account_id, comment_id, campaign_id)
            SELECT instagram_account_id, @CommentId, id FROM campaigns
            WHERE id = @Id AND instagram_account_id = @InstagramAccountId AND status = 'Ativa'
            ON CONFLICT DO NOTHING
            """, new { commentId, campaign.Id, campaign.InstagramAccountId }, cancellationToken: ct)) == 1;
    }

    public async Task FinishCommentAsync(Guid accountId, string commentId, string? sentMessageId, string? recipientId, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            WITH completed AS (
                UPDATE instagram_comment_replies SET status = CASE WHEN @Sent THEN 'Enviado' ELSE 'Falhou' END,
                    sent_message_id = @SentMessageId, recipient_id = @RecipientId
                WHERE instagram_account_id = @AccountId AND comment_id = @CommentId AND status = 'Processando'
                RETURNING campaign_id
            )
            UPDATE campaigns SET sent_count = sent_count + CASE WHEN @Sent THEN 1 ELSE 0 END,
                failed_count = failed_count + CASE WHEN @Sent THEN 0 ELSE 1 END,
                total_audience = total_audience + 1, updated_at = NOW()
            FROM completed WHERE campaigns.id = completed.campaign_id
            """, new { accountId, commentId, sent = !string.IsNullOrWhiteSpace(sentMessageId), sentMessageId, recipientId }, cancellationToken: ct));
    }

    public async Task<bool> RecordCommentCampaignResponseAsync(Guid accountId, string recipientId, string? repliedMessageId,
        Guid leadId, DateTime receivedAt, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE instagram_comment_replies SET replied_at = @ReceivedAt, lead_id = @LeadId
            WHERE (instagram_account_id, comment_id) = (
                SELECT instagram_account_id, comment_id FROM instagram_comment_replies
                WHERE instagram_account_id = @AccountId AND recipient_id = @RecipientId AND status = 'Enviado'
                  AND created_at <= @ReceivedAt AND created_at >= @ReceivedAt - INTERVAL '7 days'
                  AND (CAST(@RepliedMessageId AS text) IS NULL OR sent_message_id = @RepliedMessageId)
                ORDER BY created_at DESC, comment_id LIMIT 1
            ) AND replied_at IS NULL
            """, new { accountId, recipientId, repliedMessageId, leadId, receivedAt }, cancellationToken: ct)) == 1;
    }
}

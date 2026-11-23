
using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class InstagramWebhookEventRepository : IInstagramWebhookEventRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public InstagramWebhookEventRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> TryCreateAsync(InstagramWebhookEvent webhookEvent, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO instagram_webhook_events (
                id, tenant_id, instagram_account_id, event_id, object_type,
                entry_id, event_type, payload, status, error_message, processed_at, created_at
            )
            VALUES (
                @Id, @TenantId, @InstagramAccountId, @EventId, @ObjectType,
                @EntryId, @EventType, @Payload::jsonb, @Status, @ErrorMessage, @ProcessedAt, @CreatedAt
            ) ON CONFLICT (event_id) DO NOTHING",
            new
            {
                webhookEvent.Id,
                webhookEvent.TenantId,
                webhookEvent.InstagramAccountId,
                webhookEvent.EventId,
                webhookEvent.ObjectType,
                webhookEvent.EntryId,
                webhookEvent.EventType,
                webhookEvent.Payload,
                Status = (short)webhookEvent.Status,
                webhookEvent.ErrorMessage,
                webhookEvent.ProcessedAt,
                webhookEvent.CreatedAt
            }, cancellationToken: ct)) == 1;
    }

    public async Task UpdateStatusAsync(Guid id, WebhookEventStatus status, string? errorMessage = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE instagram_webhook_events
              SET status = @Status,
                  error_message = @ErrorMessage,
                  processed_at = CASE WHEN @Status IN (3, 4, 5) THEN NOW() ELSE processed_at END
              WHERE id = @Id",
            new { Id = id, Status = (short)status, ErrorMessage = errorMessage });
    }

    public async Task<InstagramWebhookEvent?> ClaimNextAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE instagram_webhook_events
            SET status = 4, processed_at = NOW(), error_message = 'Processamento interrompido. Confira o inbox e o Instagram.'
            WHERE status = 2 AND processing_started_at < NOW() - INTERVAL '1 hour'
            """, cancellationToken: ct));
        return await connection.QuerySingleOrDefaultAsync<InstagramWebhookEvent>(new CommandDefinition(
            """
            UPDATE instagram_webhook_events
            SET status = 2, processing_started_at = NOW()
            WHERE id = (
                SELECT id FROM instagram_webhook_events
                WHERE status = 1 ORDER BY created_at
                LIMIT 1 FOR UPDATE SKIP LOCKED
            )
            RETURNING id, payload, tenant_id AS TenantId, instagram_account_id AS InstagramAccountId
            """, cancellationToken: ct));
    }
}

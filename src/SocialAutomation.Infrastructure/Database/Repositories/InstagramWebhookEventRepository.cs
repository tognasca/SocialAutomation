
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

    public async Task<bool> ExistsAsync(string eventId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM instagram_webhook_events WHERE event_id = @EventId)",
            new { EventId = eventId });
    }

    public async Task<Guid> CreateAsync(InstagramWebhookEvent webhookEvent, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO instagram_webhook_events (
                id, tenant_id, instagram_account_id, event_id, object_type,
                entry_id, event_type, payload, status, error_message, processed_at, created_at
            )
            VALUES (
                @Id, @TenantId, @InstagramAccountId, @EventId, @ObjectType,
                @EntryId, @EventType, @Payload::jsonb, @Status, @ErrorMessage, @ProcessedAt, @CreatedAt
            )",
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
            });

        return webhookEvent.Id;
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

    public async Task<IEnumerable<InstagramWebhookEvent>> GetPendingEventsAsync(int limit = 50, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<InstagramWebhookEvent>(
            @"SELECT id, tenant_id AS TenantId, instagram_account_id AS InstagramAccountId,
                     event_id AS EventId, object_type AS ObjectType, entry_id AS EntryId,
                     event_type AS EventType, payload, status, error_message AS ErrorMessage,
                     processed_at AS ProcessedAt, created_at AS CreatedAt
              FROM instagram_webhook_events
              WHERE status = 1
              ORDER BY created_at ASC
              LIMIT @Limit",
            new { Limit = limit });
    }
}
using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class InboxRepository : IInboxRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public InboxRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Conversation?> GetConversationByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT c.id, c.tenant_id AS TenantId, c.lead_id AS LeadId, c.instagram_account_id AS InstagramAccountId,
                   c.assigned_user_id AS AssignedUserId, c.mode, c.status, c.unread_count AS UnreadCount,
                   c.last_message_preview AS LastMessagePreview, c.last_message_at AS LastMessageAt,
                   c.created_at AS CreatedAt, c.updated_at AS UpdatedAt,
                   l.name AS LeadName, l.profile_image AS LeadProfileImage, l.instagram_username AS InstagramUsername,
                   l.score_classification AS ScoreClassification, u.name AS AssignedUserName
            FROM conversations c
            INNER JOIN leads l ON l.id = c.lead_id
            LEFT JOIN users u ON u.id = c.assigned_user_id
            WHERE c.id = @Id AND c.tenant_id = @TenantId";
        return await connection.QueryFirstOrDefaultAsync<Conversation>(sql, new { Id = id, TenantId = tenantId });
    }

    public async Task<Conversation?> GetConversationByLeadIdAsync(Guid leadId, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Conversation>(
            @"SELECT id, tenant_id AS TenantId, lead_id AS LeadId, instagram_account_id AS InstagramAccountId,
                     assigned_user_id AS AssignedUserId, mode, status, unread_count AS UnreadCount,
                     last_message_preview AS LastMessagePreview, last_message_at AS LastMessageAt
              FROM conversations WHERE lead_id = @LeadId AND tenant_id = @TenantId",
            new { LeadId = leadId, TenantId = tenantId });
    }

    public async Task<(IEnumerable<Conversation> Items, int Total)> GetConversationsPagedAsync(
        Guid tenantId, int page, int pageSize, string? status = null, Guid? assignedUserId = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = "WHERE c.tenant_id = @TenantId";
        if (!string.IsNullOrWhiteSpace(status)) where += " AND c.status = @Status";
        if (assignedUserId.HasValue) where += " AND c.assigned_user_id = @AssignedUserId";

        var total = await connection.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM conversations c {where}",
            new { TenantId = tenantId, Status = status, AssignedUserId = assignedUserId });

        var sql = $@"
            SELECT c.id, c.tenant_id AS TenantId, c.lead_id AS LeadId, c.assigned_user_id AS AssignedUserId,
                   c.mode, c.status, c.unread_count AS UnreadCount, c.last_message_preview AS LastMessagePreview,
                   c.last_message_at AS LastMessageAt, l.name AS LeadName, l.profile_image AS LeadProfileImage,
                   l.instagram_username AS InstagramUsername, l.score_classification AS ScoreClassification, u.name AS AssignedUserName
            FROM conversations c
            INNER JOIN leads l ON l.id = c.lead_id
            LEFT JOIN users u ON u.id = c.assigned_user_id
            {where}
            ORDER BY c.last_message_at DESC
            LIMIT @PageSize OFFSET @Offset";

        var items = await connection.QueryAsync<Conversation>(sql, new
        {
            TenantId = tenantId,
            Status = status,
            AssignedUserId = assignedUserId,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        });

        return (items, total);
    }

    public async Task<Guid> CreateConversationAsync(Conversation conversation, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO conversations (id, tenant_id, lead_id, instagram_account_id, assigned_user_id, mode, status, unread_count, last_message_preview, last_message_at)
            VALUES (@Id, @TenantId, @LeadId, @InstagramAccountId, @AssignedUserId, @Mode, @Status, @UnreadCount, @LastMessagePreview, @LastMessageAt)";
        await connection.ExecuteAsync(sql, conversation);
        return conversation.Id;
    }

    public async Task UpdateConversationAsync(Conversation conversation, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE conversations
            SET unread_count = @UnreadCount, last_message_preview = @LastMessagePreview, last_message_at = @LastMessageAt, updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId";
        await connection.ExecuteAsync(sql, conversation);
    }

    public async Task AssignConversationAsync(Guid id, Guid tenantId, Guid? userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE conversations SET assigned_user_id = @UserId, updated_at = NOW() WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId, UserId = userId });
    }

    public async Task UpdateConversationModeAsync(Guid id, Guid tenantId, string mode, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE conversations SET mode = @Mode, updated_at = NOW() WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId, Mode = mode });
    }

    public async Task MarkAsReadAsync(Guid conversationId, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE conversations SET unread_count = 0, updated_at = NOW() WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = conversationId, TenantId = tenantId });
    }

    public async Task<IEnumerable<Message>> GetMessagesAsync(Guid conversationId, Guid tenantId, int limit = 50, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id, tenant_id AS TenantId, conversation_id AS ConversationId, sender_type AS SenderType,
                   sender_id AS SenderId, instagram_message_id AS InstagramMessageId, content, media_url AS MediaUrl,
                   status, created_at AS CreatedAt
            FROM messages
            WHERE conversation_id = @ConversationId AND tenant_id = @TenantId
            ORDER BY created_at ASC
            LIMIT @Limit";
        return await connection.QueryAsync<Message>(sql, new { ConversationId = conversationId, TenantId = tenantId, Limit = limit });
    }

    public async Task<Guid> CreateMessageAsync(Message message, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO messages (id, tenant_id, conversation_id, sender_type, sender_id, instagram_message_id, content, media_url, status, created_at)
            VALUES (@Id, @TenantId, @ConversationId, @SenderType, @SenderId, @InstagramMessageId, @Content, @MediaUrl, @Status, @CreatedAt)";
        await connection.ExecuteAsync(sql, message);
        return message.Id;
    }

    public async Task<bool> MessageExistsByInstagramIdAsync(string instagramMessageId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM messages WHERE instagram_message_id = @Id)",
            new { Id = instagramMessageId });
    }
}
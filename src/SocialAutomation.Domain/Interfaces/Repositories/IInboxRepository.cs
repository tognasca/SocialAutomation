using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IInboxRepository
{
    // Conversas
    Task<Conversation?> GetConversationByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Conversation?> GetConversationByLeadIdAsync(Guid leadId, Guid tenantId, CancellationToken ct = default);
    Task<(IEnumerable<Conversation> Items, int Total)> GetConversationsPagedAsync(Guid tenantId, int page, int pageSize, string? status = null, Guid? assignedUserId = null, CancellationToken ct = default);
    Task<Guid> CreateConversationAsync(Conversation conversation, CancellationToken ct = default);
    Task UpdateConversationAsync(Conversation conversation, CancellationToken ct = default);
    Task AssignConversationAsync(Guid id, Guid tenantId, Guid? userId, CancellationToken ct = default);
    Task UpdateConversationModeAsync(Guid id, Guid tenantId, string mode, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid conversationId, Guid tenantId, CancellationToken ct = default);

    // Mensagens
    Task<IEnumerable<Message>> GetMessagesAsync(Guid conversationId, Guid tenantId, int limit = 50, CancellationToken ct = default);
    Task<Guid> CreateMessageAsync(Message message, CancellationToken ct = default);
    Task<bool> MessageExistsByInstagramIdAsync(string instagramMessageId, CancellationToken ct = default);
}
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Application.Inbox.DTOs;
using SocialAutomation.Application.Instagram.DTOs;
using SocialAutomation.Application.Instagram.Services;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.Inbox.Services;

public class InboxService
{
    private readonly IInboxRepository _inboxRepository;
    private readonly InstagramMessageService _instagramMessageService;
    private readonly ILeadRepository _leadRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public InboxService(
        IInboxRepository inboxRepository,
        InstagramMessageService instagramMessageService,
        ILeadRepository leadRepository,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _inboxRepository = inboxRepository;
        _instagramMessageService = instagramMessageService;
        _leadRepository = leadRepository;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PagedResult<ConversationDto>>> GetConversationsAsync(PagedRequest request, string? status = null, Guid? assignedUserId = null, CancellationToken ct = default)
    {
        var (items, total) = await _inboxRepository.GetConversationsPagedAsync(
            _tenantContext.TenantId, request.Page, request.PageSize, status, assignedUserId, ct);

        var dtos = items.Select(c => new ConversationDto
        {
            Id = c.Id,
            LeadId = c.LeadId,
            LeadName = c.LeadName ?? "Desconhecido",
            InstagramUsername = c.InstagramUsername,
            LeadProfileImage = c.LeadProfileImage,
            AssignedUserId = c.AssignedUserId,
            AssignedUserName = c.AssignedUserName,
            Mode = c.Mode,
            Status = c.Status,
            UnreadCount = c.UnreadCount,
            LastMessagePreview = c.LastMessagePreview,
            LastMessageAt = c.LastMessageAt,
            ScoreClassification = c.ScoreClassification
        });

        return Result.Ok(new PagedResult<ConversationDto> { Items = dtos, Total = total, Page = request.Page, PageSize = request.PageSize });
    }

    public async Task<Result<IEnumerable<MessageDto>>> GetMessagesAsync(Guid conversationId, CancellationToken ct = default)
    {
        await _inboxRepository.MarkAsReadAsync(conversationId, _tenantContext.TenantId, ct);
        var messages = await _inboxRepository.GetMessagesAsync(conversationId, _tenantContext.TenantId, 100, ct);

        return Result.Ok(messages.Select(m => new MessageDto
        {
            Id = m.Id,
            SenderType = m.SenderType,
            SenderId = m.SenderId,
            Content = m.Content,
            Status = m.Status,
            CreatedAt = m.CreatedAt
        }));
    }

    public async Task<Result> AssignUserAsync(Guid conversationId, Guid? userId, CancellationToken ct = default)
    {
        await _inboxRepository.AssignConversationAsync(conversationId, _tenantContext.TenantId, userId, ct);
        return Result.Ok();
    }

    public async Task<Result> ChangeModeAsync(Guid conversationId, string mode, CancellationToken ct = default)
    {
        await _inboxRepository.UpdateConversationModeAsync(conversationId, _tenantContext.TenantId, mode, ct);
        return Result.Ok();
    }

    public async Task<Result<MessageDto>> SendMessageAsync(Guid conversationId, SendInboxMessageRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content)) return Result.Fail<MessageDto>("Mensagem vazia.");

        var conv = await _inboxRepository.GetConversationByIdAsync(conversationId, _tenantContext.TenantId, ct);
        if (conv == null) return Result.Fail<MessageDto>("Conversa não encontrada.");

        var lead = await _leadRepository.GetByIdAsync(conv.LeadId, _tenantContext.TenantId, ct);

        // Dispara mensagem real no Instagram
        if (conv.InstagramAccountId.HasValue && lead?.InstagramUserId != null)
        {
            var igResult = await _instagramMessageService.SendDirectMessageAsync(new SendInstagramMessageRequest
            {
                InstagramAccountId = conv.InstagramAccountId.Value,
                RecipientInstagramScopedId = lead.InstagramUserId,
                Text = request.Content
            }, ct);

            if (!igResult.Success) return Result.Fail<MessageDto>(igResult.Error!);
        }

        // Salva na nossa base
        var msg = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            ConversationId = conversationId,
            SenderType = "User",
            SenderId = _tenantContext.UserId,
            Content = request.Content.Trim(),
            CreatedAt = _dateTimeProvider.UtcNow
        };

        await _inboxRepository.CreateMessageAsync(msg, ct);

        conv.LastMessagePreview = msg.Content.Length > 50 ? msg.Content[..47] + "..." : msg.Content;
        conv.LastMessageAt = msg.CreatedAt;
        await _inboxRepository.UpdateConversationAsync(conv, ct);

        return Result.Ok(new MessageDto
        {
            Id = msg.Id,
            SenderType = msg.SenderType,
            SenderId = msg.SenderId,
            Content = msg.Content,
            Status = msg.Status,
            CreatedAt = msg.CreatedAt
        });
    }
}
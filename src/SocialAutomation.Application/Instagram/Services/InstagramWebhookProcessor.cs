
using Microsoft.Extensions.Logging;
using SocialAutomation.Application.AI.Services;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;
using System.Text.Json;

namespace SocialAutomation.Application.Instagram.Services;

public class InstagramWebhookProcessor
{
    private readonly IInstagramAccountRepository _accountRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly IInboxRepository _inboxRepository;
    private readonly IAiAgentRepository _aiAgentRepository;
    private readonly AiConversationService _aiConversationService;
    private readonly IInstagramClient _instagramClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<InstagramWebhookProcessor> _logger;

    public InstagramWebhookProcessor(
        IInstagramAccountRepository accountRepository,
        ILeadRepository leadRepository,
        IInboxRepository inboxRepository,
        IAiAgentRepository aiAgentRepository,
        AiConversationService aiConversationService,
        IInstagramClient instagramClient,
        IDateTimeProvider dateTimeProvider,
        ILogger<InstagramWebhookProcessor> logger)
    {
        _accountRepository = accountRepository;
        _leadRepository = leadRepository;
        _inboxRepository = inboxRepository;
        _aiAgentRepository = aiAgentRepository;
        _aiConversationService = aiConversationService;
        _instagramClient = instagramClient;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task ProcessPayloadAsync(string payload, Guid tenantId, Guid instagramAccountId, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        if (!root.TryGetProperty("messaging", out var messagingArray) || messagingArray.ValueKind != JsonValueKind.Array)
            return;

        var account = await _accountRepository.GetByIdAsync(instagramAccountId, tenantId, ct);
        if (account?.Status != SocialAutomation.Domain.Enums.InstagramAccountStatus.Active)
            throw new InvalidOperationException("Conta do Instagram inativa ou desvinculada.");

        foreach (var messaging in messagingArray.EnumerateArray())
        {
            if (!messaging.TryGetProperty("message", out var messageEl))
                continue;

            var messageId = messageEl.TryGetProperty("mid", out var midEl) ? midEl.GetString() ?? "" : "";
            var text = messageEl.TryGetProperty("text", out var textEl) ? textEl.GetString() ?? "" : "";
            var senderId = messaging.TryGetProperty("sender", out var senderEl) && senderEl.TryGetProperty("id", out var sId)
                ? sId.GetString() ?? "" : "";

            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(senderId))
                continue;

            // Ignorar mensagens enviadas pela própria página
            if (senderId == account.InstagramBusinessId || senderId == account.PageId)
                continue;

            if (string.IsNullOrEmpty(messageId) || (messageEl.TryGetProperty("is_echo", out var echo) &&
                echo.ValueKind == JsonValueKind.True))
                continue;

            var now = _dateTimeProvider.UtcNow;
            var receivedAt = messaging.TryGetProperty("timestamp", out var stamp) &&
                stamp.ValueKind == JsonValueKind.Number && stamp.TryGetInt64(out var ms) &&
                ms >= 0 && ms <= new DateTimeOffset(now).ToUnixTimeMilliseconds()
                    ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime
                    : DateTime.UnixEpoch;

            // 3. Localiza ou Cria o Lead no CRM automaticamente
            var lead = await _leadRepository.GetByInstagramUserIdAsync(senderId, account.TenantId, account.Id, ct);
            if (lead == null)
            {
                // Busca perfil público na Meta API
                var profile = await _instagramClient.GetInstagramUserProfileAsync(senderId, account.AccessToken, ct);

                lead = new Lead
                {
                    Id = Guid.NewGuid(),
                    TenantId = account.TenantId,
                    InstagramAccountId = account.Id,
                    Name = profile?.Name ?? profile?.Username ?? $"Lead Instagram ({senderId[..Math.Min(6, senderId.Length)]})",
                    InstagramUsername = profile?.Username ?? senderId,
                    InstagramUserId = senderId,
                    ProfileImage = profile?.ProfilePic,
                    Source = "instagram_dm",
                    Status = "Novo",
                    PipelineStage = "Novo",
                    Score = 0,
                    ScoreClassification = "Frio",
                    CreatedAt = _dateTimeProvider.UtcNow,
                    UpdatedAt = _dateTimeProvider.UtcNow
                };

                await _leadRepository.CreateAsync(lead, ct);
                _logger.LogInformation("Novo Lead criado automaticamente via DM: {Name} (ID: {LeadId})", lead.Name, lead.Id);
            }

            // 4. Localiza ou Cria a Conversa no Inbox
            var conversation = await _inboxRepository.GetConversationByLeadIdAsync(lead.Id, account.TenantId, ct);
            if (conversation == null)
            {
                // Checa se há agente de IA padrão ativo para iniciar em IA_ATIVA ou HUMANO
                var defaultAgent = await _aiAgentRepository.GetDefaultAsync(account.TenantId, ct);
                var defaultMode = (defaultAgent != null && defaultAgent.IsActive) ? "IA_ATIVA" : "HUMANO";

                conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    TenantId = account.TenantId,
                    LeadId = lead.Id,
                    InstagramAccountId = account.Id,
                    Mode = defaultMode,
                    Status = "Aberto",
                    UnreadCount = 0,
                    LastMessagePreview = text.Length > 50 ? text[..47] + "..." : text,
                    LastMessageAt = receivedAt,
                    CreatedAt = _dateTimeProvider.UtcNow,
                    UpdatedAt = _dateTimeProvider.UtcNow
                };

                await _inboxRepository.CreateConversationAsync(conversation, ct);
            }

            if (conversation.InstagramAccountId != account.Id)
                throw new InvalidOperationException("Conversa vinculada a outra conta do Instagram.");

            // 5. Salva a mensagem recebida no banco
            var incomingMsg = new Message
            {
                Id = Guid.NewGuid(),
                TenantId = account.TenantId,
                ConversationId = conversation.Id,
                SenderType = "Lead",
                InstagramMessageId = messageId,
                Content = text,
                Status = "Delivered",
                CreatedAt = receivedAt
            };

            if (!await _inboxRepository.TryCreateInstagramMessageAsync(incomingMsg, account.Id, ct))
                continue;

            // Atualiza preview da conversa no Inbox
            if (incomingMsg.CreatedAt >= conversation.LastMessageAt)
            {
                conversation.LastMessagePreview = text.Length > 50 ? text[..47] + "..." : text;
                conversation.LastMessageAt = incomingMsg.CreatedAt;
            }
            conversation.UnreadCount++;
            await _inboxRepository.UpdateConversationAsync(conversation, ct);

            // 6. Se a conversa estiver em modo IA_ATIVA -> Executa a IA e responde
            if (receivedAt >= now.AddHours(-24) &&
                string.Equals(conversation.Mode, "IA_ATIVA", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Processando mensagem com IA para a conversa {ConversationId}", conversation.Id);

                var aiResult = await _aiConversationService.ProcessIncomingMessageAsync(
                    account.TenantId, conversation.Id, text, ct, saveReply: false);

                if (!aiResult.Success)
                    throw new InvalidOperationException("O agente de IA não conseguiu preparar a resposta.");

                if (!string.IsNullOrWhiteSpace(aiResult.Data))
                {
                    // Envia resposta no Instagram do cliente via Graph API Oficial
                    var sendResult = await _instagramClient.SendTextMessageAsync(
                        account.PageId,
                        account.AccessToken,
                        senderId,
                        aiResult.Data,
                        ct);

                    var sent = !string.IsNullOrEmpty(sendResult?.MessageId);
                    await _inboxRepository.CreateMessageAsync(new Message
                    {
                        Id = Guid.NewGuid(),
                        TenantId = account.TenantId,
                        ConversationId = conversation.Id,
                        SenderType = "AI",
                        InstagramMessageId = sendResult?.MessageId,
                        Content = aiResult.Data,
                        Status = sent ? "Sent" : "Failed",
                        CreatedAt = _dateTimeProvider.UtcNow
                    }, ct);
                    if (!sent)
                        throw new InvalidOperationException("A Meta não confirmou o envio da resposta automática.");
                }
            }
        }
    }
}
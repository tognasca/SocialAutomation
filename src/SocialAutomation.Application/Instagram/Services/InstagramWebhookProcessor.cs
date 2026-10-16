
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

    public async Task ProcessPayloadAsync(string payload, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        if (!root.TryGetProperty("messaging", out var messagingArray) || messagingArray.ValueKind != JsonValueKind.Array)
            return;

        var entryId = root.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";

        // 1. Identifica a conta conectada
        var account = await _accountRepository.GetByBusinessIdAsync(entryId, ct)
                      ?? await _accountRepository.GetByPageIdAsync(entryId, ct);

        if (account == null)
        {
            _logger.LogWarning("Nenhuma conta ativas do Instagram encontrada para o EntryId {EntryId}", entryId);
            return;
        }

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

            // 2. Idempotência: Checa se mensagem já foi processada
            if (!string.IsNullOrEmpty(messageId) && await _inboxRepository.MessageExistsByInstagramIdAsync(messageId, ct))
            {
                _logger.LogDebug("Mensagem duplicada ignorada: {MessageId}", messageId);
                continue;
            }

            // 3. Localiza ou Cria o Lead no CRM automaticamente
            var lead = await _leadRepository.GetByInstagramUserIdAsync(senderId, account.TenantId, ct);
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
                    LastMessageAt = _dateTimeProvider.UtcNow,
                    CreatedAt = _dateTimeProvider.UtcNow,
                    UpdatedAt = _dateTimeProvider.UtcNow
                };

                await _inboxRepository.CreateConversationAsync(conversation, ct);
            }

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
                CreatedAt = _dateTimeProvider.UtcNow
            };

            await _inboxRepository.CreateMessageAsync(incomingMsg, ct);

            // Atualiza preview da conversa no Inbox
            conversation.LastMessagePreview = text.Length > 50 ? text[..47] + "..." : text;
            conversation.LastMessageAt = incomingMsg.CreatedAt;
            conversation.UnreadCount++;
            await _inboxRepository.UpdateConversationAsync(conversation, ct);

            // 6. Se a conversa estiver em modo IA_ATIVA -> Executa a IA e responde
            if (string.Equals(conversation.Mode, "IA_ATIVA", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Processando mensagem com IA para a conversa {ConversationId}", conversation.Id);

                var aiResult = await _aiConversationService.ProcessIncomingMessageAsync(
                    account.TenantId, conversation.Id, text, ct);

                if (aiResult.Success && !string.IsNullOrWhiteSpace(aiResult.Data))
                {
                    // Envia resposta no Instagram do cliente via Graph API Oficial
                    var sendResult = await _instagramClient.SendTextMessageAsync(
                        account.PageId,
                        account.AccessToken,
                        senderId,
                        aiResult.Data,
                        ct);

                    if (sendResult != null && !string.IsNullOrEmpty(sendResult.MessageId))
                    {
                        _logger.LogInformation("Resposta da IA enviada com sucesso no Instagram! MessageId: {MessageId}", sendResult.MessageId);
                    }
                    else
                    {
                        _logger.LogWarning("IA gerou resposta, mas houve falha ao enviar via Graph API Meta. Verifique janela de 24h.");
                    }
                }
            }
        }
    }
}
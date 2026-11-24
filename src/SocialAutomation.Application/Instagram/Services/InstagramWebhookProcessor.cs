
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
    private readonly ICampaignRepository _campaigns;
    private readonly ILogger<InstagramWebhookProcessor> _logger;

    public InstagramWebhookProcessor(
        IInstagramAccountRepository accountRepository,
        ILeadRepository leadRepository,
        IInboxRepository inboxRepository,
        IAiAgentRepository aiAgentRepository,
        AiConversationService aiConversationService,
        IInstagramClient instagramClient,
        IDateTimeProvider dateTimeProvider,
        ILogger<InstagramWebhookProcessor> logger, ICampaignRepository campaigns)
    {
        _accountRepository = accountRepository;
        _leadRepository = leadRepository;
        _inboxRepository = inboxRepository;
        _aiAgentRepository = aiAgentRepository;
        _aiConversationService = aiConversationService;
        _instagramClient = instagramClient;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _campaigns = campaigns;
    }

    public async Task ProcessPayloadAsync(string payload, Guid tenantId, Guid instagramAccountId, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var account = await _accountRepository.GetByIdAsync(instagramAccountId, tenantId, ct);
        if (account?.Status != SocialAutomation.Domain.Enums.InstagramAccountStatus.Active)
            throw new InvalidOperationException("Conta do Instagram inativa ou desvinculada.");

        if (root.TryGetProperty("changes", out var changes) && changes.ValueKind == JsonValueKind.Array &&
            root.TryGetProperty("time", out var eventTime) && eventTime.ValueKind == JsonValueKind.Number &&
            eventTime.TryGetInt64(out var timestamp) &&
            timestamp >= new DateTimeOffset(_dateTimeProvider.UtcNow.AddDays(-7)).ToUnixTimeSeconds() &&
            timestamp <= new DateTimeOffset(_dateTimeProvider.UtcNow.AddMinutes(5)).ToUnixTimeSeconds())
            await ProcessCommentsAsync(changes, account, ct);

        if (!root.TryGetProperty("messaging", out var messagingArray) || messagingArray.ValueKind != JsonValueKind.Array)
            return;

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

            var replyTo = messageEl.TryGetProperty("reply_to", out var reference) ? ReadString(reference, "mid") : null;
            await _campaigns.RecordCommentCampaignResponseAsync(account.Id, senderId, replyTo, lead.Id, receivedAt, ct);

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
                // Human requests go through the existing handoff path.
                var asksForHuman = System.Text.RegularExpressions.Regex.IsMatch(text,
                    @"\b(humano|atendente|parar|cancelar|descadastrar)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (asksForHuman && await _campaigns.HasSalesFlowAsync(account.TenantId, account.Id, lead.Id, ct))
                {
                    await _inboxRepository.UpdateConversationModeAsync(conversation.Id, account.TenantId, "HUMANO", ct);
                    continue;
                }

                var reply = await _campaigns.AdvanceSalesFlowAsync(account.TenantId, account.Id, lead.Id, text, now, ct);
                if (reply is null)
                {
                    _logger.LogInformation("Processando mensagem com IA para a conversa {ConversationId}", conversation.Id);
                    var aiResult = await _aiConversationService.ProcessIncomingMessageAsync(
                        account.TenantId, conversation.Id, text, ct, saveReply: false);

                    if (!aiResult.Success)
                        throw new InvalidOperationException("O agente de IA não conseguiu preparar a resposta.");

                    reply = aiResult.Data;
                }

                if (!string.IsNullOrWhiteSpace(reply))
                {
                    // Envia resposta no Instagram do cliente via Graph API Oficial
                    var sendResult = await _instagramClient.SendTextMessageAsync(
                        account.PageId,
                        account.AccessToken,
                        senderId,
                        reply,
                        ct);

                    var sent = !string.IsNullOrEmpty(sendResult?.MessageId);
                    await _inboxRepository.CreateMessageAsync(new Message
                    {
                        Id = Guid.NewGuid(),
                        TenantId = account.TenantId,
                        ConversationId = conversation.Id,
                        SenderType = "AI",
                        InstagramMessageId = sendResult?.MessageId,
                        Content = reply,
                        Status = sent ? "Sent" : "Failed",
                        CreatedAt = _dateTimeProvider.UtcNow
                    }, ct);
                    if (!sent)
                        throw new InvalidOperationException("A Meta não confirmou o envio da resposta automática.");
                }
            }
        }
    }

    private async Task ProcessCommentsAsync(JsonElement changes, InstagramAccount account, CancellationToken ct)
    {
        foreach (var change in changes.EnumerateArray())
        {
            if (ReadString(change, "field") != "comments" ||
                !change.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Object ||
                !value.TryGetProperty("from", out var author) ||
                !value.TryGetProperty("media", out var media))
                continue;

            var authorId = ReadString(author, "id");
            if (string.IsNullOrEmpty(authorId) || authorId == account.InstagramBusinessId || authorId == account.PageId ||
                string.Equals(ReadString(author, "username"), account.Username, StringComparison.OrdinalIgnoreCase))
                continue;

            var commentId = ReadString(value, "comment_id") ?? ReadString(value, "id");
            var mediaId = ReadString(media, "id");
            var text = ReadString(value, "text");
            if (string.IsNullOrEmpty(commentId) || commentId.Length > 200 ||
                string.IsNullOrEmpty(mediaId) || string.IsNullOrWhiteSpace(text))
                continue;

            var words = System.Text.RegularExpressions.Regex.Matches(text.Normalize().ToLowerInvariant(), @"[\p{L}\p{M}\p{N}_]+")
                .Select(match => match.Value).ToHashSet(StringComparer.Ordinal);
            var campaigns = await _campaigns.GetActiveCommentCampaignsAsync(account.TenantId, account.Id, mediaId, ct);
            var campaign = campaigns.FirstOrDefault(c => c.CommentKeyword is { } keyword && words.Contains(keyword));
            if (campaign is null || !await _campaigns.TryClaimCommentAsync(campaign, commentId, ct))
                continue;

            var result = await _instagramClient.SendPrivateReplyAsync(account.PageId, account.AccessToken,
                commentId, campaign.MessageTemplate, ct);
            var sent = !string.IsNullOrWhiteSpace(result?.MessageId);
            await _campaigns.FinishCommentAsync(account.Id, commentId, result?.MessageId, result?.RecipientId, CancellationToken.None);
            if (!sent)
                _logger.LogWarning("A Meta não confirmou a resposta privada da campanha {CampaignId}.", campaign.Id);
        }
    }

    private static string? ReadString(JsonElement element, string key) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var value) &&
        value.ValueKind is JsonValueKind.String or JsonValueKind.Number ? value.ToString() : null;
}

using SocialAutomation.Application.Common;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;
using SocialAutomation.Domain.Models.AI;
using SocialAutomation.Infrastructure.AI;
using System.Diagnostics;
using System.Text.Json;

namespace SocialAutomation.Application.AI.Services;

public class AiConversationService
{
    private readonly IAIProviderFactory _providerFactory;
    private readonly IAiAgentRepository _agentRepository;
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly IInboxRepository _inboxRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IAiMessageLogRepository _aiLogRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AiConversationService(
        IAIProviderFactory providerFactory,
        IAiAgentRepository agentRepository,
        IKnowledgeRepository knowledgeRepository,
        IInboxRepository inboxRepository,
        ILeadRepository leadRepository,
        ITagRepository tagRepository,
        IAiMessageLogRepository aiLogRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _providerFactory = providerFactory;
        _agentRepository = agentRepository;
        _knowledgeRepository = knowledgeRepository;
        _inboxRepository = inboxRepository;
        _leadRepository = leadRepository;
        _tagRepository = tagRepository;
        _aiLogRepository = aiLogRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<string>> ProcessIncomingMessageAsync(
        Guid tenantId,
        Guid conversationId,
        string incomingMessage,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var conversation = await _inboxRepository.GetConversationByIdAsync(conversationId, tenantId, ct);
        if (conversation == null)
            return Result.Fail<string>("Conversa não encontrada.");

        if (!string.Equals(conversation.Mode, "IA_ATIVA", StringComparison.OrdinalIgnoreCase))
            return Result.Fail<string>("Conversa não está em modo IA_ATIVA.");

        var agent = await _agentRepository.GetDefaultAsync(tenantId, ct);
        if (agent == null || !agent.IsActive)
            return Result.Fail<string>("Nenhum agente de IA ativo configurado.");

        var lead = await _leadRepository.GetByIdAsync(conversation.LeadId, tenantId, ct);
        var history = (await _inboxRepository.GetMessagesAsync(conversationId, tenantId, 12, ct)).ToList();
        var knowledge = (await _knowledgeRepository.SearchChunksAsync(tenantId, incomingMessage, 5, ct)).ToList();

        var systemPrompt = BuildSystemPrompt(agent, lead, knowledge);
        var messages = new List<AiChatMessage>
        {
            new() { Role = "system", Content = systemPrompt }
        };

        foreach (var h in history.TakeLast(10))
        {
            messages.Add(new AiChatMessage
            {
                Role = h.SenderType is "Lead" ? "user" : "assistant",
                Content = h.Content
            });
        }

        messages.Add(new AiChatMessage { Role = "user", Content = incomingMessage });

        // Instrui JSON estruturado
        messages.Add(new AiChatMessage
        {
            Role = "system",
            Content = """
            Responda APENAS em JSON válido no formato:
            {
              "reply": "texto para o cliente",
              "intent": "general|price|quote|purchase|complaint|human_request",
              "shouldHandoff": false,
              "handoffReason": null,
              "suggestedTags": ["tag1"],
              "scoreDelta": 0,
              "leadStatusSuggestion": null
            }
            """
        });

        var provider = _providerFactory.GetProvider(agent.ModelProvider);
        var completion = await provider.CompleteAsync(new AiCompletionRequest
        {
            Provider = agent.ModelProvider,
            Model = agent.ModelName,
            Temperature = agent.Temperature,
            MaxTokens = agent.MaxTokens,
            Messages = messages
        }, ct);

        sw.Stop();

        var decision = ParseDecision(completion.Content, incomingMessage);

        await _aiLogRepository.CreateAsync(new AiMessageLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConversationId = conversationId,
            LeadId = conversation.LeadId,
            AgentId = agent.Id,
            Provider = agent.ModelProvider,
            ModelName = agent.ModelName,
            InputTokens = completion.InputTokens,
            OutputTokens = completion.OutputTokens,
            Intent = decision.Intent,
            ShouldHandoff = decision.ShouldHandoff,
            HandoffReason = decision.HandoffReason,
            RequestPayload = JsonSerializer.Serialize(new { incomingMessage }),
            ResponsePayload = completion.RawJson,
            Success = completion.Success,
            ErrorMessage = completion.Error,
            DurationMs = sw.ElapsedMilliseconds,
            CreatedAt = _dateTimeProvider.UtcNow
        }, ct);

        if (!completion.Success)
            return Result.Fail<string>(completion.Error ?? "Falha no provider de IA.");

        // Atualiza CRM
        if (decision.ScoreDelta != 0 && lead != null)
        {
            var newScore = Math.Max(0, lead.Score + decision.ScoreDelta);
            var classification = newScore switch
            {
                < 30 => "Frio",
                < 60 => "Morno",
                < 90 => "Quente",
                _ => "Alta Intenção"
            };
            await _leadRepository.UpdateScoreAsync(lead.Id, tenantId, newScore, classification, ct);
        }

        foreach (var tagName in decision.SuggestedTags.Where(t => !string.IsNullOrWhiteSpace(t)).Take(3))
        {
            var existing = await _tagRepository.GetByNameAsync(tagName, tenantId, ct);
            Guid tagId;
            if (existing == null)
            {
                tagId = Guid.NewGuid();
                await _tagRepository.CreateAsync(new Tag
                {
                    Id = tagId,
                    TenantId = tenantId,
                    Name = tagName.Trim(),
                    Color = "#8B5CF6",
                    CreatedAt = _dateTimeProvider.UtcNow
                }, ct);
            }
            else tagId = existing.Id;

            await _tagRepository.AssignTagToLeadAsync(conversation.LeadId, tagId, ct);
        }

        // Handoff
        if (decision.ShouldHandoff || IsHardHandoff(incomingMessage, agent.HandoffRules))
        {
            await _inboxRepository.UpdateConversationModeAsync(conversationId, tenantId, "HUMANO", ct);

            await _inboxRepository.CreateMessageAsync(new Message
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ConversationId = conversationId,
                SenderType = "System",
                Content = $"Transferido para atendimento humano. Motivo: {decision.HandoffReason ?? "solicitação do cliente"}",
                Status = "Sent",
                CreatedAt = _dateTimeProvider.UtcNow
            }, ct);

            return Result.Ok(decision.Reply);
        }

        // Salva resposta da IA no inbox
        var aiMessage = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConversationId = conversationId,
            SenderType = "AI",
            Content = decision.Reply,
            Status = "Sent",
            CreatedAt = _dateTimeProvider.UtcNow
        };
        await _inboxRepository.CreateMessageAsync(aiMessage, ct);

        conversation.LastMessagePreview = decision.Reply.Length > 50 ? decision.Reply[..47] + "..." : decision.Reply;
        conversation.LastMessageAt = aiMessage.CreatedAt;
        conversation.UnreadCount = 0;
        await _inboxRepository.UpdateConversationAsync(conversation, ct);

        return Result.Ok(decision.Reply);
    }

    private static string BuildSystemPrompt(AiAgent agent, Lead? lead, List<KnowledgeChunk> knowledge)
    {
        var kbText = knowledge.Count == 0
            ? "Sem trechos específicos da base."
            : string.Join("\n- ", knowledge.Select(k => k.Content));

        return $"""
        Você é {agent.Name}, agente comercial de atendimento via Instagram.
        Personalidade: {agent.Personality}
        Tom: {agent.Tone}
        Objetivo: {agent.Objective}
        Empresa: {agent.CompanyName}
        Descrição: {agent.CompanyDescription}
        Produtos/Serviços: {agent.ProductsServices}
        Preços: {agent.PricingInfo}
        Horários: {agent.BusinessHours}
        FAQ: {agent.Faq}
        Instruções: {agent.Instructions}
        Regras de handoff: {agent.HandoffRules}

        Lead atual:
        - Nome: {lead?.Name}
        - Instagram: {lead?.InstagramUsername}
        - Score: {lead?.Score} ({lead?.ScoreClassification})
        - Status: {lead?.Status}

        Base de conhecimento relevante:
        - {kbText}

        Regras:
        1. Seja útil, objetivo e consultivo.
        2. Nunca invente preço/política se não estiver na base.
        3. Se cliente pedir humano, reclamar, pedir orçamento complexo ou demonstrar compra imediata, marque shouldHandoff=true.
        4. Responda em português do Brasil.
        """;
    }

    private static AiDecisionResult ParseDecision(string content, string fallbackUserMessage)
    {
        try
        {
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = content[jsonStart..(jsonEnd + 1)];
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return new AiDecisionResult
                {
                    Reply = root.TryGetProperty("reply", out var r) ? r.GetString() ?? "Posso te ajudar?" : "Posso te ajudar?",
                    Intent = root.TryGetProperty("intent", out var i) ? i.GetString() ?? "general" : "general",
                    ShouldHandoff = root.TryGetProperty("shouldHandoff", out var h) && h.GetBoolean(),
                    HandoffReason = root.TryGetProperty("handoffReason", out var hr) ? hr.GetString() : null,
                    SuggestedTags = root.TryGetProperty("suggestedTags", out var tags) && tags.ValueKind == JsonValueKind.Array
                        ? tags.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList()
                        : new List<string>(),
                    ScoreDelta = root.TryGetProperty("scoreDelta", out var sd) ? sd.GetInt32() : 0,
                    LeadStatusSuggestion = root.TryGetProperty("leadStatusSuggestion", out var ls) ? ls.GetString() : null
                };
            }
        }
        catch
        {
            // fallback abaixo
        }

        // fallback heurístico
        var lower = fallbackUserMessage.ToLowerInvariant();
        var handoff = lower.Contains("atendente") || lower.Contains("humano") || lower.Contains("pessoa");
        return new AiDecisionResult
        {
            Reply = string.IsNullOrWhiteSpace(content) ? "Recebi sua mensagem! Em instantes te retorno." : content,
            Intent = handoff ? "human_request" : "general",
            ShouldHandoff = handoff,
            HandoffReason = handoff ? "Cliente solicitou atendimento humano" : null,
            ScoreDelta = lower.Contains("preço") || lower.Contains("valor") ? 10 : 5
        };
    }

    private static bool IsHardHandoff(string message, string? rules)
    {
        var lower = message.ToLowerInvariant();
        if (lower.Contains("quero falar com atendente") || lower.Contains("fala com humano") || lower.Contains("reclamação"))
            return true;

        if (!string.IsNullOrWhiteSpace(rules))
        {
            foreach (var token in rules.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (lower.Contains(token.ToLowerInvariant()))
                    return true;
            }
        }

        return false;
    }
}
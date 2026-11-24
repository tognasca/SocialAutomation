using System.Text.Json;
using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class AiMessageLogRepository : IAiMessageLogRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public AiMessageLogRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task CreateAsync(AiMessageLog log, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO ai_message_logs (
                id, tenant_id, conversation_id, lead_id, agent_id, provider, model_name,
                input_tokens, output_tokens, intent, should_handoff, handoff_reason,
                request_payload, response_payload, success, error_message, duration_ms, created_at
              ) VALUES (
                @Id, @TenantId, @ConversationId, @LeadId, @AgentId, @Provider, @ModelName,
                @InputTokens, @OutputTokens, @Intent, @ShouldHandoff, @HandoffReason,
                @RequestPayload::jsonb, @ResponsePayload::jsonb, @Success, @ErrorMessage, @DurationMs, @CreatedAt
              )",
            new
            {
                log.Id,
                log.TenantId,
                log.ConversationId,
                log.LeadId,
                log.AgentId,
                log.Provider,
                log.ModelName,
                log.InputTokens,
                log.OutputTokens,
                log.Intent,
                log.ShouldHandoff,
                log.HandoffReason,
                RequestPayload = NormalizePayload(log.RequestPayload),
                ResponsePayload = NormalizePayload(log.ResponsePayload),
                log.Success,
                log.ErrorMessage,
                log.DurationMs,
                log.CreatedAt
            });
    }

    private static string NormalizePayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return "{}";

        try
        {
            using var document = JsonDocument.Parse(payload);
            return payload;
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(payload);
        }
    }
}

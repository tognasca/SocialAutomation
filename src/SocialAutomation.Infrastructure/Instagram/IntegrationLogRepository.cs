using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class IntegrationLogRepository : IIntegrationLogRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public IntegrationLogRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task CreateAsync(IntegrationLog log, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO integration_logs (
                id, tenant_id, provider, operation, endpoint, http_method,
                status_code, request_payload, response_payload, success, error_message,
                duration_ms, created_at
            )
            VALUES (
                @Id, @TenantId, @Provider, @Operation, @Endpoint, @HttpMethod,
                @StatusCode, @RequestPayload::jsonb, @ResponsePayload::jsonb, @Success, @ErrorMessage,
                @DurationMs, @CreatedAt
            )",
            new
            {
                log.Id,
                log.TenantId,
                log.Provider,
                log.Operation,
                log.Endpoint,
                log.HttpMethod,
                log.StatusCode,
                RequestPayload = log.RequestPayload ?? "{}",
                ResponsePayload = log.ResponsePayload ?? "{}",
                log.Success,
                log.ErrorMessage,
                log.DurationMs,
                log.CreatedAt
            });
    }
}
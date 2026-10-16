using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class AiAgentRepository : IAiAgentRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public AiAgentRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<AiAgent>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<AiAgent>(
            @"SELECT id, tenant_id AS TenantId, name, is_active AS IsActive, is_default AS IsDefault,
                     personality, tone, objective, company_name AS CompanyName, company_description AS CompanyDescription,
                     products_services AS ProductsServices, pricing_info AS PricingInfo, business_hours AS BusinessHours,
                     faq, instructions, handoff_rules AS HandoffRules, model_provider AS ModelProvider,
                     model_name AS ModelName, temperature, max_tokens AS MaxTokens,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM ai_agents WHERE tenant_id = @TenantId ORDER BY is_default DESC, name",
            new { TenantId = tenantId });
    }

    public async Task<AiAgent?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<AiAgent>(
            @"SELECT id, tenant_id AS TenantId, name, is_active AS IsActive, is_default AS IsDefault,
                     personality, tone, objective, company_name AS CompanyName, company_description AS CompanyDescription,
                     products_services AS ProductsServices, pricing_info AS PricingInfo, business_hours AS BusinessHours,
                     faq, instructions, handoff_rules AS HandoffRules, model_provider AS ModelProvider,
                     model_name AS ModelName, temperature, max_tokens AS MaxTokens,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM ai_agents WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task<AiAgent?> GetDefaultAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<AiAgent>(
            @"SELECT id, tenant_id AS TenantId, name, is_active AS IsActive, is_default AS IsDefault,
                     personality, tone, objective, company_name AS CompanyName, company_description AS CompanyDescription,
                     products_services AS ProductsServices, pricing_info AS PricingInfo, business_hours AS BusinessHours,
                     faq, instructions, handoff_rules AS HandoffRules, model_provider AS ModelProvider,
                     model_name AS ModelName, temperature, max_tokens AS MaxTokens,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM ai_agents
              WHERE tenant_id = @TenantId AND is_active = TRUE
              ORDER BY is_default DESC, created_at ASC
              LIMIT 1",
            new { TenantId = tenantId });
    }

    public async Task<Guid> CreateAsync(AiAgent agent, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO ai_agents (
                id, tenant_id, name, is_active, is_default, personality, tone, objective,
                company_name, company_description, products_services, pricing_info, business_hours,
                faq, instructions, handoff_rules, model_provider, model_name, temperature, max_tokens, created_at, updated_at
              ) VALUES (
                @Id, @TenantId, @Name, @IsActive, @IsDefault, @Personality, @Tone, @Objective,
                @CompanyName, @CompanyDescription, @ProductsServices, @PricingInfo, @BusinessHours,
                @Faq, @Instructions, @HandoffRules, @ModelProvider, @ModelName, @Temperature, @MaxTokens, @CreatedAt, @UpdatedAt
              )", agent);
        return agent.Id;
    }

    public async Task UpdateAsync(AiAgent agent, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE ai_agents SET
                name = @Name, is_active = @IsActive, personality = @Personality, tone = @Tone,
                objective = @Objective, company_name = @CompanyName, company_description = @CompanyDescription,
                products_services = @ProductsServices, pricing_info = @PricingInfo, business_hours = @BusinessHours,
                faq = @Faq, instructions = @Instructions, handoff_rules = @HandoffRules,
                model_provider = @ModelProvider, model_name = @ModelName, temperature = @Temperature,
                max_tokens = @MaxTokens, updated_at = @UpdatedAt
              WHERE id = @Id AND tenant_id = @TenantId", agent);
    }

    public async Task SetDefaultAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE ai_agents SET is_default = FALSE WHERE tenant_id = @TenantId",
            new { TenantId = tenantId });
        await connection.ExecuteAsync(
            "UPDATE ai_agents SET is_default = TRUE, updated_at = NOW() WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM ai_agents WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }
}
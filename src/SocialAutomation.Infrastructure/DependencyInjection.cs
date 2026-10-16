
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;
using SocialAutomation.Infrastructure.AI;
using SocialAutomation.Infrastructure.Cache;
using SocialAutomation.Infrastructure.Database;
using SocialAutomation.Infrastructure.Database.Repositories;
using SocialAutomation.Infrastructure.Instagram;
using SocialAutomation.Infrastructure.Security;
using SocialAutomation.Infrastructure.Services;
using SocialAutomation.Infrastructure.Storage;
using StackExchange.Redis;

namespace SocialAutomation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database - Suporta PostgreSQL, Postgres ou DefaultConnection
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? configuration.GetConnectionString("Postgres")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:PostgreSQL"]
            ?? configuration["ConnectionStrings:Postgres"]
            ?? throw new InvalidOperationException("PostgreSQL connection string not configured. Verifique a chave 'ConnectionStrings:PostgreSQL' no appsettings.json.");

        services.AddSingleton(new DbConnectionFactory(connectionString));
        services.AddSingleton<MigrationRunner>();

        // Repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IInstagramAccountRepository, InstagramAccountRepository>();
        services.AddScoped<IInstagramWebhookEventRepository, InstagramWebhookEventRepository>();
        services.AddScoped<IIntegrationLogRepository, IntegrationLogRepository>();
        services.AddScoped<IAiAgentRepository, AiAgentRepository>();
        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
        services.AddScoped<IAiMessageLogRepository, AiMessageLogRepository>();

        services.AddHttpClient<OpenAIProvider>();
        services.AddScoped<IAIProvider>(sp => sp.GetRequiredService<OpenAIProvider>());
        services.AddScoped<IAIProviderFactory, AIProviderFactory>();
        // Repositories do CRM
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ILeadActivityRepository, LeadActivityRepository>();
        services.AddScoped<ICrmTaskRepository, CrmTaskRepository>();
        services.AddScoped<ILeadScoreRuleRepository, LeadScoreRuleRepository>();
        services.AddScoped<IInboxRepository, InboxRepository>();
        services.AddScoped<IAutomationRepository, AutomationRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<IStorageService, LocalStorageService>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();



        // HttpClients
        services.AddHttpClient<IInstagramClient, InstagramClient>();

        // Redis
        var redisConnectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            try
            {
                return ConnectionMultiplexer.Connect(redisConnectionString);
            }
            catch
            {
                // Fallback para não quebrar inicialização caso Redis esteja subindo
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(options);
            }
        });
        services.AddScoped<ICacheService, RedisCacheService>();

        // Security
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenGenerator>();

        // Services
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ITenantContext, TenantContext>();

        return services;
    }
}
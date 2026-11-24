using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Infrastructure.Database;
using SocialAutomation.Infrastructure.Database.Repositories;

namespace SocialAutomation.IntegrationTests.Repositories;

public sealed class InstagramPostgresTests
{
    [PostgresFact]
    public async Task WebhookAndPublicationClaimsAreDurableAndExclusive()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("SOCIALAUTOMATION_TEST_POSTGRES"))
        {
            Database = $"socialautomation_test_{Guid.NewGuid():N}"
        };
        var factory = new DbConnectionFactory(connectionString.ConnectionString);
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSQL"] = connectionString.ConnectionString,
            ["Instagram:TokenEncryptionKey"] = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
        }).Build();
        var migrations = Path.Combine(AppContext.BaseDirectory, "migrations");
        var runner = new MigrationRunner(factory, NullLogger<MigrationRunner>.Instance, settings);

        try
        {
            await runner.RunAsync(migrations);
            await runner.RunAsync(migrations);
            using var connection = factory.CreateConnection();
            var tenantId = await connection.ExecuteScalarAsync<Guid>(
                "INSERT INTO tenants (name, slug) VALUES ('Test', 'test') RETURNING id");
            var logs = new AiMessageLogRepository(factory);
            await logs.CreateAsync(new AiMessageLog
            {
                Id = Guid.NewGuid(), TenantId = tenantId, Provider = "ollama", ModelName = "qwen2.5:1.5b",
                RequestPayload = """{"incomingMessage":"Olá"}""", ResponsePayload = string.Empty,
                Success = true, CreatedAt = DateTime.UtcNow
            });
            Assert.True(await connection.ExecuteScalarAsync<bool>(
                """
                SELECT request_payload->>'incomingMessage' = 'Olá' AND response_payload = '{}'::jsonb
                FROM ai_message_logs WHERE tenant_id = @tenantId
                """, new { tenantId }));
            var repository = new InstagramAccountRepository(factory, settings);
            var accountId = await repository.CreateOrUpdateAsync(new InstagramAccount
            {
                Id = Guid.NewGuid(), TenantId = tenantId, InstagramBusinessId = "instagram",
                PageId = "page", PageName = "Page", Username = "test", AccessToken = "page-token",
                UserAccessToken = "test-user-token", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            Assert.Equal("test-user-token", (await repository.GetByIdAsync(accountId, tenantId))?.UserAccessToken);
            Assert.NotEqual("test-user-token", await connection.ExecuteScalarAsync<string>(
                "SELECT user_access_token FROM instagram_accounts WHERE id = @accountId", new { accountId }));
            Assert.Null(await repository.GetByIdAsync(accountId, Guid.NewGuid()));

            var queue = new InstagramWebhookEventRepository(factory);
            var item = new InstagramWebhookEvent
            {
                Id = Guid.NewGuid(), TenantId = tenantId, InstagramAccountId = accountId, EventId = "stable",
                EntryId = "instagram", ObjectType = "instagram", EventType = "meta.entry",
                Payload = """{"id":"instagram"}""", Status = WebhookEventStatus.Received, CreatedAt = DateTime.UtcNow
            };
            var inserts = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => queue.TryCreateAsync(item)));
            Assert.Single(inserts, value => value);
            var claims = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => queue.ClaimNextAsync()));
            Assert.Equal(item.Id, Assert.Single(claims.OfType<InstagramWebhookEvent>()).Id);
            await queue.UpdateStatusAsync(item.Id, WebhookEventStatus.Processed);
            Assert.Null(await queue.ClaimNextAsync());

            var content = new ContentRepository(factory);
            var contentId = await content.CreateAsync(new ContentItem
            {
                Id = Guid.NewGuid(), TenantId = tenantId, InstagramAccountId = accountId,
                Title = "Post", Status = "Agendado", ScheduledAt = DateTime.UtcNow.AddMinutes(-1),
                MediaUrls = ["https://example.test/post.jpg"], CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            var publicationClaims = await Task.WhenAll(Enumerable.Range(0, 5)
                .Select(_ => content.TryClaimPublicationAsync(contentId, tenantId, default)));
            Assert.Single(publicationClaims, value => value);
            await content.UpdateStatusAsync(contentId, tenantId, "Publicado", "confirmed-meta-id");
            Assert.Equal("confirmed-meta-id", (await content.GetByIdAsync(contentId, tenantId))?.InstagramPublicationId);

            var leadId = await connection.ExecuteScalarAsync<Guid>(
                "INSERT INTO leads (tenant_id, instagram_account_id, name, instagram_user_id) VALUES (@tenantId, @accountId, 'Lead', 'recipient') RETURNING id",
                new { tenantId, accountId });
            var conversationId = await connection.ExecuteScalarAsync<Guid>(
                """
                INSERT INTO conversations (tenant_id, lead_id, instagram_account_id)
                VALUES (@tenantId, @leadId, @accountId) RETURNING id
                """, new { tenantId, leadId, accountId });
            var inbox = new InboxRepository(factory);
            var message = new Message
            {
                Id = Guid.NewGuid(), TenantId = tenantId, ConversationId = conversationId, SenderType = "Lead",
                InstagramMessageId = "mid", Content = "Olá", Status = "Delivered", CreatedAt = DateTime.UtcNow
            };
            var messages = await Task.WhenAll(Enumerable.Range(0, 5)
                .Select(_ => inbox.TryCreateInstagramMessageAsync(message, accountId, default)));
            Assert.Single(messages, value => value);
            Assert.True(await inbox.HasRecentInstagramConversationAsync(tenantId, accountId, "recipient", DateTime.UtcNow.AddHours(-24), default));
            Assert.False(await inbox.HasRecentInstagramConversationAsync(tenantId, accountId, "another-recipient", DateTime.UtcNow.AddHours(-24), default));
            var prospects = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
                new LeadRepository(factory).SaveInstagramProspectAsync(tenantId, "revenda_teste", "Revenda Teste", default)));
            Assert.Single(prospects.Distinct());

            var campaigns = new CampaignRepository(factory);
            var campaign = new Campaign
            {
                Id = Guid.NewGuid(), TenantId = tenantId, InstagramAccountId = accountId,
                Name = "Demonstração", InstagramMediaId = "12345", CommentKeyword = "demo",
                MessageTemplate = "Quer agendar uma demonstração?", Status = "Ativa",
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            await campaigns.CreateAsync(campaign);
            Assert.Empty(await campaigns.GetActiveCommentCampaignsAsync(Guid.NewGuid(), accountId, "12345", default));
            Assert.Equal(campaign.Id, Assert.Single(await campaigns.GetActiveCommentCampaignsAsync(tenantId, accountId, "12345", default)).Id);
            var commentClaims = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => campaigns.TryClaimCommentAsync(campaign, "comment", default)));
            Assert.Single(commentClaims, claimed => claimed);
            await campaigns.FinishCommentAsync(accountId, "comment", "outgoing-mid", "recipient", default);
            Assert.True(await campaigns.RecordCommentCampaignResponseAsync(accountId, "recipient", "outgoing-mid", leadId, DateTime.UtcNow, default));
            Assert.False(await campaigns.RecordCommentCampaignResponseAsync(accountId, "recipient", "outgoing-mid", leadId, DateTime.UtcNow, default));
            var measured = await campaigns.GetByIdAsync(campaign.Id, tenantId);
            Assert.Equal(1, measured!.SentCount);
            Assert.Equal(1, measured.ReplyCount);

        }
        finally
        {
            var database = connectionString.Database;
            connectionString.Database = "postgres";
            await using var admin = new NpgsqlConnection(connectionString.ConnectionString);
            await admin.ExecuteAsync($"""DROP DATABASE IF EXISTS "{database}" WITH (FORCE)""");
        }
    }

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SOCIALAUTOMATION_TEST_POSTGRES")))
                Skip = "Defina SOCIALAUTOMATION_TEST_POSTGRES para validar com PostgreSQL isolado.";
        }
    }
}

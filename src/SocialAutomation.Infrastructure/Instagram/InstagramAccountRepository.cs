
using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class InstagramAccountRepository : IInstagramAccountRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public InstagramAccountRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<InstagramAccount?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<InstagramAccount>(
            @"SELECT id, tenant_id AS TenantId, instagram_business_id AS InstagramBusinessId,
                     instagram_user_id AS InstagramUserId, page_id AS PageId, page_name AS PageName,
                     username, name, profile_picture_url AS ProfilePictureUrl,
                     access_token AS AccessToken, token_expires_at AS TokenExpiresAt,
                     status, webhook_subscribed AS WebhookSubscribed, scopes,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM instagram_accounts
              WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task<InstagramAccount?> GetByBusinessIdAsync(string businessId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<InstagramAccount>(
            @"SELECT id, tenant_id AS TenantId, instagram_business_id AS InstagramBusinessId,
                     instagram_user_id AS InstagramUserId, page_id AS PageId, page_name AS PageName,
                     username, name, profile_picture_url AS ProfilePictureUrl,
                     access_token AS AccessToken, token_expires_at AS TokenExpiresAt,
                     status, webhook_subscribed AS WebhookSubscribed, scopes,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM instagram_accounts
              WHERE instagram_business_id = @BusinessId AND status = 1",
            new { BusinessId = businessId });
    }

    public async Task<InstagramAccount?> GetByPageIdAsync(string pageId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<InstagramAccount>(
            @"SELECT id, tenant_id AS TenantId, instagram_business_id AS InstagramBusinessId,
                     instagram_user_id AS InstagramUserId, page_id AS PageId, page_name AS PageName,
                     username, name, profile_picture_url AS ProfilePictureUrl,
                     access_token AS AccessToken, token_expires_at AS TokenExpiresAt,
                     status, webhook_subscribed AS WebhookSubscribed, scopes,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM instagram_accounts
              WHERE page_id = @PageId AND status = 1",
            new { PageId = pageId });
    }

    public async Task<IEnumerable<InstagramAccount>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<InstagramAccount>(
            @"SELECT id, tenant_id AS TenantId, instagram_business_id AS InstagramBusinessId,
                     instagram_user_id AS InstagramUserId, page_id AS PageId, page_name AS PageName,
                     username, name, profile_picture_url AS ProfilePictureUrl,
                     access_token AS AccessToken, token_expires_at AS TokenExpiresAt,
                     status, webhook_subscribed AS WebhookSubscribed, scopes,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM instagram_accounts
              WHERE tenant_id = @TenantId
              ORDER BY created_at DESC",
            new { TenantId = tenantId });
    }

    public async Task<Guid> CreateOrUpdateAsync(InstagramAccount account, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO instagram_accounts (
                id, tenant_id, instagram_business_id, instagram_user_id, page_id, page_name,
                username, name, profile_picture_url, access_token, token_expires_at, status,
                webhook_subscribed, scopes, created_at, updated_at
            )
            VALUES (
                @Id, @TenantId, @InstagramBusinessId, @InstagramUserId, @PageId, @PageName,
                @Username, @Name, @ProfilePictureUrl, @AccessToken, @TokenExpiresAt, @Status,
                @WebhookSubscribed, @Scopes, @CreatedAt, @UpdatedAt
            )
            ON CONFLICT (tenant_id, instagram_business_id)
            DO UPDATE SET
                page_id = EXCLUDED.page_id,
                page_name = EXCLUDED.page_name,
                username = EXCLUDED.username,
                name = EXCLUDED.name,
                profile_picture_url = EXCLUDED.profile_picture_url,
                access_token = EXCLUDED.access_token,
                token_expires_at = EXCLUDED.token_expires_at,
                status = EXCLUDED.status,
                webhook_subscribed = EXCLUDED.webhook_subscribed,
                scopes = EXCLUDED.scopes,
                updated_at = EXCLUDED.updated_at
            RETURNING id;";

        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            account.Id,
            account.TenantId,
            account.InstagramBusinessId,
            account.InstagramUserId,
            account.PageId,
            account.PageName,
            account.Username,
            account.Name,
            account.ProfilePictureUrl,
            account.AccessToken,
            account.TokenExpiresAt,
            Status = (short)account.Status,
            account.WebhookSubscribed,
            account.Scopes,
            account.CreatedAt,
            account.UpdatedAt
        });
    }

    public async Task UpdateStatusAsync(Guid id, Guid tenantId, InstagramAccountStatus status, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE instagram_accounts
              SET status = @Status, updated_at = NOW()
              WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId, Status = (short)status });
    }

    public async Task UpdateWebhookSubscribedAsync(Guid id, bool subscribed, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE instagram_accounts
              SET webhook_subscribed = @Subscribed, updated_at = NOW()
              WHERE id = @Id",
            new { Id = id, Subscribed = subscribed });
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM instagram_accounts WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM instagram_accounts WHERE tenant_id = @TenantId AND status = 1",
            new { TenantId = tenantId });
    }
}
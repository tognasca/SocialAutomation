using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class ContentRepository : IContentRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public ContentRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ContentItem>> GetAllByTenantAsync(Guid tenantId, string? type = null, string? status = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var where = "WHERE tenant_id = @TenantId";
        if (!string.IsNullOrWhiteSpace(type)) where += " AND type = @Type";
        if (!string.IsNullOrWhiteSpace(status)) where += " AND status = @Status";

        var sql = $@"
            SELECT id, tenant_id AS TenantId, instagram_account_id AS InstagramAccountId,
                   type, title, caption, hashtags, cta, media_urls AS MediaUrls,
                   media_format AS MediaFormat, status, scheduled_at AS ScheduledAt,
                   published_at AS PublishedAt, instagram_publication_id AS InstagramPublicationId,
                   error_message AS ErrorMessage, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM content_items
            {where}
            ORDER BY COALESCE(scheduled_at, created_at) DESC";

        var items = (await connection.QueryAsync<ContentItem>(sql, new { TenantId = tenantId, Type = type, Status = status })).ToList();

        foreach (var item in items.Where(x => x.Type == "Reel"))
        {
            item.Scenes = (await GetScenesByContentIdAsync(item.Id, ct)).ToList();
        }

        return items;
    }

    public async Task<ContentItem?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id, tenant_id AS TenantId, instagram_account_id AS InstagramAccountId,
                   type, title, caption, hashtags, cta, media_urls AS MediaUrls,
                   media_format AS MediaFormat, status, scheduled_at AS ScheduledAt,
                   published_at AS PublishedAt, instagram_publication_id AS InstagramPublicationId,
                   error_message AS ErrorMessage, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM content_items
            WHERE id = @Id AND tenant_id = @TenantId";

        var item = await connection.QueryFirstOrDefaultAsync<ContentItem>(sql, new { Id = id, TenantId = tenantId });

        if (item != null && item.Type == "Reel")
        {
            item.Scenes = (await GetScenesByContentIdAsync(item.Id, ct)).ToList();
        }

        return item;
    }

    public async Task<Guid> CreateAsync(ContentItem item, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO content_items (
                id, tenant_id, instagram_account_id, type, title, caption, hashtags, cta,
                media_urls, media_format, status, scheduled_at, published_at, instagram_publication_id,
                error_message, created_at, updated_at
            ) VALUES (
                @Id, @TenantId, @InstagramAccountId, @Type, @Title, @Caption, @Hashtags, @Cta,
                @MediaUrls, @MediaFormat, @Status, @ScheduledAt, @PublishedAt, @InstagramPublicationId,
                @ErrorMessage, @CreatedAt, @UpdatedAt
            )";

        await connection.ExecuteAsync(sql, item);
        return item.Id;
    }

    public async Task UpdateAsync(ContentItem item, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE content_items
            SET instagram_account_id = @InstagramAccountId,
                type = @Type, title = @Title, caption = @Caption, hashtags = @Hashtags,
                cta = @Cta, media_urls = @MediaUrls, media_format = @MediaFormat,
                status = @Status, scheduled_at = @ScheduledAt, updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId";

        await connection.ExecuteAsync(sql, item);
    }

    public async Task UpdateStatusAsync(Guid id, Guid tenantId, string status, string? publicationId = null, string? errorMessage = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE content_items
              SET status = @Status,
                  instagram_publication_id = COALESCE(@PublicationId, instagram_publication_id),
                  error_message = @ErrorMessage,
                  published_at = CASE WHEN @Status = 'Publicado' THEN NOW() ELSE published_at END,
                  updated_at = NOW()
              WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId, Status = status, PublicationId = publicationId, ErrorMessage = errorMessage });
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM content_items WHERE id = @Id AND tenant_id = @TenantId", new { Id = id, TenantId = tenantId });
    }

    public async Task SaveReelScenesAsync(Guid contentItemId, Guid tenantId, IEnumerable<ReelScene> scenes, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM reel_scenes WHERE content_item_id = @ContentItemId AND tenant_id = @TenantId", new { ContentItemId = contentItemId, TenantId = tenantId });

        var sql = @"
            INSERT INTO reel_scenes (id, tenant_id, content_item_id, scene_order, script_text, visual_description, duration_seconds, created_at)
            VALUES (@Id, @TenantId, @ContentItemId, @SceneOrder, @ScriptText, @VisualDescription, @DurationSeconds, NOW())";

        foreach (var scene in scenes)
        {
            await connection.ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ContentItemId = contentItemId,
                scene.SceneOrder,
                scene.ScriptText,
                scene.VisualDescription,
                scene.DurationSeconds
            });
        }
    }

    public async Task<IEnumerable<ReelScene>> GetScenesByContentIdAsync(Guid contentItemId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ReelScene>(
            @"SELECT id, tenant_id AS TenantId, content_item_id AS ContentItemId,
                     scene_order AS SceneOrder, script_text AS ScriptText,
                     visual_description AS VisualDescription, duration_seconds AS DurationSeconds, created_at AS CreatedAt
              FROM reel_scenes WHERE content_item_id = @ContentItemId ORDER BY scene_order ASC",
            new { ContentItemId = contentItemId });
    }

    public async Task<IEnumerable<ContentItem>> GetPendingScheduledContentAsync(int limit = 20, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ContentItem>(
            @"SELECT id, tenant_id AS TenantId, instagram_account_id AS InstagramAccountId,
                     type, title, caption, hashtags, cta, media_urls AS MediaUrls,
                     media_format AS MediaFormat, status, scheduled_at AS ScheduledAt
              FROM content_items
              WHERE status = 'Agendado' AND scheduled_at <= NOW()
              ORDER BY scheduled_at ASC
              LIMIT @Limit",
            new { Limit = limit });
    }
}
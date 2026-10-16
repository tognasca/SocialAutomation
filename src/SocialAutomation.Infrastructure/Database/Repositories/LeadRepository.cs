
using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public LeadRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Lead?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT l.id, l.tenant_id AS TenantId, l.instagram_account_id AS InstagramAccountId,
                   l.name, l.instagram_username AS InstagramUsername, l.instagram_user_id AS InstagramUserId,
                   l.profile_image AS ProfileImage, l.phone, l.email, l.company, l.source,
                   l.status, l.pipeline_stage AS PipelineStage, l.pipeline_order AS PipelineOrder,
                   l.deal_value AS DealValue, l.owner_user_id AS OwnerUserId, l.score,
                   l.score_classification AS ScoreClassification, l.notes,
                   l.last_interaction_at AS LastInteractionAt, l.next_follow_up_at AS NextFollowUpAt,
                   l.created_at AS CreatedAt, l.updated_at AS UpdatedAt,
                   u.name AS OwnerName
            FROM leads l
            LEFT JOIN users u ON u.id = l.owner_user_id
            WHERE l.id = @Id AND l.tenant_id = @TenantId";

        var lead = await connection.QueryFirstOrDefaultAsync<Lead>(sql, new { Id = id, TenantId = tenantId });

        if (lead != null)
        {
            var tagsSql = @"
                SELECT t.id, t.tenant_id AS TenantId, t.name, t.color, t.created_at AS CreatedAt
                FROM tags t
                INNER JOIN lead_tags lt ON lt.tag_id = t.id
                WHERE lt.lead_id = @LeadId";

            lead.Tags = (await connection.QueryAsync<Tag>(tagsSql, new { LeadId = lead.Id })).ToList();
        }

        return lead;
    }

    public async Task<Lead?> GetByInstagramUserIdAsync(string instagramUserId, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Lead>(
            @"SELECT id, tenant_id AS TenantId, name, instagram_username AS InstagramUsername,
                     instagram_user_id AS InstagramUserId, score, status, pipeline_stage AS PipelineStage
              FROM leads
              WHERE instagram_user_id = @InstagramUserId AND tenant_id = @TenantId",
            new { InstagramUserId = instagramUserId, TenantId = tenantId });
    }

    public async Task<Lead?> GetByInstagramUsernameAsync(string username, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Lead>(
            @"SELECT id, tenant_id AS TenantId, name, instagram_username AS InstagramUsername,
                     instagram_user_id AS InstagramUserId, score, status, pipeline_stage AS PipelineStage
              FROM leads
              WHERE LOWER(instagram_username) = LOWER(@Username) AND tenant_id = @TenantId",
            new { Username = username.TrimStart('@'), TenantId = tenantId });
    }

    public async Task<(IEnumerable<Lead> Items, int Total)> GetPagedAsync(
        Guid tenantId, int page, int pageSize, string? search = null, string? status = null,
        string? pipelineStage = null, Guid? tagId = null, Guid? ownerUserId = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var where = "WHERE l.tenant_id = @TenantId";
        if (!string.IsNullOrWhiteSpace(search))
            where += " AND (l.name ILIKE @Search OR l.instagram_username ILIKE @Search OR l.email ILIKE @Search OR l.phone ILIKE @Search)";
        if (!string.IsNullOrWhiteSpace(status))
            where += " AND l.status = @Status";
        if (!string.IsNullOrWhiteSpace(pipelineStage))
            where += " AND l.pipeline_stage = @PipelineStage";
        if (ownerUserId.HasValue)
            where += " AND l.owner_user_id = @OwnerUserId";
        if (tagId.HasValue)
            where += " AND EXISTS(SELECT 1 FROM lead_tags lt WHERE lt.lead_id = l.id AND lt.tag_id = @TagId)";

        var countSql = $"SELECT COUNT(*) FROM leads l {where}";
        var total = await connection.QueryFirstOrDefaultAsync<int>(countSql, new
        {
            TenantId = tenantId,
            Search = $"%{search}%",
            Status = status,
            PipelineStage = pipelineStage,
            OwnerUserId = ownerUserId,
            TagId = tagId
        });

        var sql = $@"
            SELECT l.id, l.tenant_id AS TenantId, l.name, l.instagram_username AS InstagramUsername,
                   l.profile_image AS ProfileImage, l.phone, l.email, l.company, l.source,
                   l.status, l.pipeline_stage AS PipelineStage, l.deal_value AS DealValue,
                   l.owner_user_id AS OwnerUserId, l.score, l.score_classification AS ScoreClassification,
                   l.last_interaction_at AS LastInteractionAt, l.next_follow_up_at AS NextFollowUpAt,
                   l.created_at AS CreatedAt, u.name AS OwnerName
            FROM leads l
            LEFT JOIN users u ON u.id = l.owner_user_id
            {where}
            ORDER BY l.created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        var leads = (await connection.QueryAsync<Lead>(sql, new
        {
            TenantId = tenantId,
            Search = $"%{search}%",
            Status = status,
            PipelineStage = pipelineStage,
            OwnerUserId = ownerUserId,
            TagId = tagId,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        })).ToList();

        // Popular Tags em lote
        if (leads.Any())
        {
            var leadIds = leads.Select(x => x.Id).ToArray();
            var tagsSql = @"
                SELECT lt.lead_id AS LeadId, t.id, t.name, t.color
                FROM tags t
                INNER JOIN lead_tags lt ON lt.tag_id = t.id
                WHERE lt.lead_id = ANY(@LeadIds)";

            var tagsLookup = (await connection.QueryAsync<(Guid LeadId, Guid Id, string Name, string Color)>(
                tagsSql, new { LeadIds = leadIds })).ToLookup(x => x.LeadId);

            foreach (var l in leads)
            {
                if (tagsLookup.Contains(l.Id))
                {
                    l.Tags = tagsLookup[l.Id].Select(t => new Tag { Id = t.Id, Name = t.Name, Color = t.Color }).ToList();
                }
            }
        }

        return (leads, total);
    }

    public async Task<IEnumerable<Lead>> GetByPipelineStageAsync(Guid tenantId, string stage, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT l.id, l.tenant_id AS TenantId, l.name, l.instagram_username AS InstagramUsername,
                   l.profile_image AS ProfileImage, l.deal_value AS DealValue, l.score,
                   l.score_classification AS ScoreClassification, l.pipeline_stage AS PipelineStage,
                   l.pipeline_order AS PipelineOrder, l.last_interaction_at AS LastInteractionAt,
                   l.next_follow_up_at AS NextFollowUpAt, u.name AS OwnerName
            FROM leads l
            LEFT JOIN users u ON u.id = l.owner_user_id
            WHERE l.tenant_id = @TenantId AND l.pipeline_stage = @Stage
            ORDER BY l.pipeline_order ASC, l.updated_at DESC";

        var leads = (await connection.QueryAsync<Lead>(sql, new { TenantId = tenantId, Stage = stage })).ToList();

        if (leads.Any())
        {
            var leadIds = leads.Select(x => x.Id).ToArray();
            var tagsSql = @"
                SELECT lt.lead_id AS LeadId, t.id, t.name, t.color
                FROM tags t
                INNER JOIN lead_tags lt ON lt.tag_id = t.id
                WHERE lt.lead_id = ANY(@LeadIds)";

            var tagsLookup = (await connection.QueryAsync<(Guid LeadId, Guid Id, string Name, string Color)>(
                tagsSql, new { LeadIds = leadIds })).ToLookup(x => x.LeadId);

            foreach (var l in leads)
            {
                if (tagsLookup.Contains(l.Id))
                {
                    l.Tags = tagsLookup[l.Id].Select(t => new Tag { Id = t.Id, Name = t.Name, Color = t.Color }).ToList();
                }
            }
        }

        return leads;
    }

    public async Task<Guid> CreateAsync(Lead lead, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO leads (
                id, tenant_id, instagram_account_id, name, instagram_username,
                instagram_user_id, profile_image, phone, email, company,
                source, status, pipeline_stage, pipeline_order, deal_value,
                owner_user_id, score, score_classification, notes,
                last_interaction_at, next_follow_up_at, created_at, updated_at
            )
            VALUES (
                @Id, @TenantId, @InstagramAccountId, @Name, @InstagramUsername,
                @InstagramUserId, @ProfileImage, @Phone, @Email, @Company,
                @Source, @Status, @PipelineStage, @PipelineOrder, @DealValue,
                @OwnerUserId, @Score, @ScoreClassification, @Notes,
                @LastInteractionAt, @NextFollowUpAt, @CreatedAt, @UpdatedAt
            );";

        await connection.ExecuteAsync(sql, lead);
        return lead.Id;
    }

    public async Task UpdateAsync(Lead lead, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE leads
            SET name = @Name,
                instagram_username = @InstagramUsername,
                phone = @Phone,
                email = @Email,
                company = @Company,
                status = @Status,
                pipeline_stage = @PipelineStage,
                deal_value = @DealValue,
                owner_user_id = @OwnerUserId,
                notes = @Notes,
                updated_at = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId";

        await connection.ExecuteAsync(sql, lead);
    }

    public async Task UpdatePipelineStageAsync(Guid id, Guid tenantId, string stage, int order, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE leads
              SET pipeline_stage = @Stage, pipeline_order = @Order, status = @Stage, updated_at = NOW()
              WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId, Stage = stage, Order = order });
    }

    public async Task UpdateScoreAsync(Guid id, Guid tenantId, int score, string classification, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE leads
              SET score = @Score, score_classification = @Classification, updated_at = NOW()
              WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId, Score = score, Classification = classification });
    }

    public async Task UpdateNextFollowUpAsync(Guid id, Guid tenantId, DateTime? nextFollowUp, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE leads
              SET next_follow_up_at = @NextFollowUp, updated_at = NOW()
              WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId, NextFollowUp = nextFollowUp });
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM leads WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task<Dictionary<string, int>> GetCountByStagesAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<(string Stage, int Count)>(
            @"SELECT pipeline_stage AS Stage, COUNT(*) AS Count
              FROM leads
              WHERE tenant_id = @TenantId
              GROUP BY pipeline_stage",
            new { TenantId = tenantId });

        return result.ToDictionary(x => x.Stage, x => x.Count);
    }
}
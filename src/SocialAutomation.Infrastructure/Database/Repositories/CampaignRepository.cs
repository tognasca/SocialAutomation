
using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public partial class CampaignRepository : ICampaignRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public CampaignRepository(DbConnectionFactory connectionFactory, Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
    {
        _connectionFactory = connectionFactory;
        _proposalBaseUrl = configuration?["App:PublicApiUrl"] ?? "https://api-socialautomation.ademartognasca.com.br";
    }

    public async Task<IEnumerable<Campaign>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Campaign>(
            @"SELECT id, tenant_id AS TenantId, instagram_account_id AS InstagramAccountId, name,
                     instagram_media_id AS InstagramMediaId, comment_keyword AS CommentKeyword,
                     message_template AS MessageTemplate, segment_filter AS SegmentFilter, status,
                     scheduled_at AS ScheduledAt, total_audience AS TotalAudience, sent_count AS SentCount,
                     failed_count AS FailedCount,
                     (SELECT count(*)::int FROM instagram_comment_replies WHERE campaign_id = campaigns.id AND replied_at IS NOT NULL) AS ReplyCount,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM campaigns WHERE tenant_id = @TenantId ORDER BY created_at DESC",
            new { TenantId = tenantId });
    }

    public async Task<Campaign?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Campaign>(
            @"SELECT id, tenant_id AS TenantId, instagram_account_id AS InstagramAccountId, name,
                     instagram_media_id AS InstagramMediaId, comment_keyword AS CommentKeyword,
                     message_template AS MessageTemplate, segment_filter AS SegmentFilter, status,
                     scheduled_at AS ScheduledAt, total_audience AS TotalAudience, sent_count AS SentCount,
                     failed_count AS FailedCount,
                     (SELECT count(*)::int FROM instagram_comment_replies WHERE campaign_id = campaigns.id AND replied_at IS NOT NULL) AS ReplyCount,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM campaigns WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task<Guid> CreateAsync(Campaign campaign, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO campaigns (id, tenant_id, instagram_account_id, name, message_template, segment_filter, status, scheduled_at, total_audience, created_at, updated_at, instagram_media_id, comment_keyword)
              VALUES (@Id, @TenantId, @InstagramAccountId, @Name, @MessageTemplate, @SegmentFilter::jsonb, @Status, @ScheduledAt, @TotalAudience, @CreatedAt, @UpdatedAt, @InstagramMediaId, @CommentKeyword)",
            new
            {
                campaign.Id,
                campaign.TenantId,
                campaign.InstagramAccountId,
                campaign.Name,
                campaign.MessageTemplate,
                SegmentFilter = campaign.SegmentFilter ?? "{}",
                campaign.Status,
                campaign.ScheduledAt,
                campaign.TotalAudience,
                campaign.CreatedAt,
                campaign.UpdatedAt,
                campaign.InstagramMediaId,
                campaign.CommentKeyword
            });

        return campaign.Id;
    }

    public async Task UpdateStatusAsync(Guid id, Guid tenantId, string status, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE campaigns SET status = @Status, updated_at = NOW() WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId, Status = status });
    }

    public async Task CreateRecipientsAsync(IEnumerable<CampaignRecipient> recipients, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            INSERT INTO campaign_recipients (id, tenant_id, campaign_id, lead_id, status, created_at)
            VALUES (@Id, @TenantId, @CampaignId, @LeadId, @Status, NOW())";

        foreach (var r in recipients)
        {
            await connection.ExecuteAsync(sql, r);
        }
    }

    public async Task<IEnumerable<CampaignRecipient>> GetPendingRecipientsAsync(Guid campaignId, int limit = 50, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<CampaignRecipient>(
            @"SELECT id, tenant_id AS TenantId, campaign_id AS CampaignId, lead_id AS LeadId, status
              FROM campaign_recipients WHERE campaign_id = @CampaignId AND status = 'Pendente' LIMIT @Limit",
            new { CampaignId = campaignId, Limit = limit });
    }

    public async Task UpdateRecipientStatusAsync(Guid recipientId, string status, string? error = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE campaign_recipients
              SET status = @Status, error_message = @Error, sent_at = CASE WHEN @Status = 'Enviado' THEN NOW() ELSE sent_at END
              WHERE id = @Id",
            new { Id = recipientId, Status = status, Error = error });
    }

    public async Task<IEnumerable<Campaign>> GetActiveCommentCampaignsAsync(Guid tenantId, Guid accountId, string mediaId, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Campaign>(new CommandDefinition(
            """
            SELECT id, instagram_account_id AS InstagramAccountId, comment_keyword AS CommentKeyword,
                   message_template AS MessageTemplate
            FROM campaigns
            WHERE tenant_id = @TenantId AND instagram_account_id = @AccountId
              AND instagram_media_id = @MediaId AND status = 'Ativa'
            ORDER BY char_length(comment_keyword) DESC, created_at, id
            """, new { tenantId, accountId, mediaId }, cancellationToken: ct));
    }

    public async Task<bool> TryClaimCommentAsync(Campaign campaign, string commentId, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO instagram_comment_replies (instagram_account_id, comment_id, campaign_id)
            SELECT instagram_account_id, @CommentId, id FROM campaigns
            WHERE id = @Id AND instagram_account_id = @InstagramAccountId AND status = 'Ativa'
            ON CONFLICT DO NOTHING
            """, new { commentId, campaign.Id, campaign.InstagramAccountId }, cancellationToken: ct)) == 1;
    }

    public async Task FinishCommentAsync(Guid accountId, string commentId, string? sentMessageId, string? recipientId, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            WITH completed AS (
                UPDATE instagram_comment_replies SET status = CASE WHEN @Sent THEN 'Enviado' ELSE 'Falhou' END,
                    sent_message_id = @SentMessageId, recipient_id = @RecipientId
                WHERE instagram_account_id = @AccountId AND comment_id = @CommentId AND status = 'Processando'
                RETURNING campaign_id
            )
            UPDATE campaigns SET sent_count = sent_count + CASE WHEN @Sent THEN 1 ELSE 0 END,
                failed_count = failed_count + CASE WHEN @Sent THEN 0 ELSE 1 END,
                total_audience = total_audience + 1, updated_at = NOW()
            FROM completed WHERE campaigns.id = completed.campaign_id
            """, new { accountId, commentId, sent = !string.IsNullOrWhiteSpace(sentMessageId), sentMessageId, recipientId }, cancellationToken: ct));
    }

    public async Task<bool> RecordCommentCampaignResponseAsync(Guid accountId, string recipientId, string? repliedMessageId,
        Guid leadId, DateTime receivedAt, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE instagram_comment_replies SET replied_at = @ReceivedAt, lead_id = @LeadId
            WHERE (instagram_account_id, comment_id) = (
                SELECT instagram_account_id, comment_id FROM instagram_comment_replies
                WHERE instagram_account_id = @AccountId AND recipient_id = @RecipientId AND status = 'Enviado'
                  AND created_at <= @ReceivedAt AND created_at >= @ReceivedAt - INTERVAL '7 days'
                  AND (CAST(@RepliedMessageId AS text) IS NULL OR sent_message_id = @RepliedMessageId)
                ORDER BY created_at DESC, comment_id LIMIT 1
            ) AND replied_at IS NULL
            """, new { accountId, recipientId, repliedMessageId, leadId, receivedAt }, cancellationToken: ct)) == 1;
    }

    public async Task<CampaignSalesSettings?> GetSalesSettingsAsync(Guid tenantId, Guid campaignId, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        var args = new { tenantId, campaignId };
        var questions = await connection.QuerySingleOrDefaultAsync<string[]>(new CommandDefinition(
            """
            SELECT COALESCE(qualification_questions, ARRAY[]::text[]) FROM campaigns
            WHERE tenant_id = @TenantId AND id = @CampaignId AND instagram_media_id IS NOT NULL
            """, args, cancellationToken: ct));
        if (questions is null)
            return null;

        var slots = await connection.QueryAsync<CampaignDemoSlot>(new CommandDefinition(
            """
            SELECT id, starts_at AS StartsAt, booked_at AS BookedAt FROM campaign_demo_slots
            WHERE tenant_id = @TenantId AND campaign_id = @CampaignId AND starts_at > NOW()
            ORDER BY starts_at
            """, args, cancellationToken: ct));
        return new CampaignSalesSettings(questions, slots);
    }

    public async Task<bool> SaveSalesSettingsAsync(Guid tenantId, Guid campaignId, string[] questions, DateTime[] newSlots, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var args = new { tenantId, campaignId, questions, newSlots };
        var updated = await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE campaigns SET qualification_questions = @Questions, updated_at = NOW()
            WHERE tenant_id = @TenantId AND id = @CampaignId AND instagram_media_id IS NOT NULL
            """, args, transaction, cancellationToken: ct));
        if (updated == 0)
            return false;

        var inserted = await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO campaign_demo_slots (tenant_id, campaign_id, starts_at)
            SELECT @TenantId, @CampaignId, s FROM unnest(@NewSlots) AS s
            ON CONFLICT (tenant_id, starts_at) DO NOTHING
            """, args, transaction, cancellationToken: ct));
        if (inserted != newSlots.Length)
            return false;

        transaction.Commit();
        return true;
    }

    public async Task<bool> HasSalesFlowAsync(Guid tenantId, Guid accountId, Guid leadId, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<bool>(new CommandDefinition(
            """
            SELECT EXISTS (
                SELECT 1 FROM instagram_comment_replies r JOIN campaigns c ON c.id = r.campaign_id
                WHERE c.tenant_id = @TenantId AND r.instagram_account_id = @AccountId AND r.lead_id = @LeadId
                  AND r.replied_at IS NOT NULL AND c.status = 'Ativa' AND cardinality(c.qualification_questions) > 0
            )
            """, new { tenantId, accountId, leadId }, cancellationToken: ct));
    }

    public async Task<string?> AdvanceSalesFlowAsync(Guid tenantId, Guid accountId, Guid leadId, string message, DateTime now, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var args = new { tenantId, accountId, leadId, now };
        var campaign = await connection.QueryFirstOrDefaultAsync<SalesCampaign>(new CommandDefinition(
            """
            SELECT c.id, c.name, c.qualification_questions AS Questions, c.proposal_plans::text AS ProposalPlans
            FROM instagram_comment_replies r
            JOIN campaigns c ON c.id = r.campaign_id AND c.instagram_account_id = r.instagram_account_id
            JOIN leads l ON l.id = r.lead_id AND l.tenant_id = c.tenant_id
            WHERE c.tenant_id = @TenantId AND r.instagram_account_id = @AccountId AND r.lead_id = @LeadId
              AND r.replied_at IS NOT NULL AND c.status = 'Ativa'
            ORDER BY r.replied_at DESC LIMIT 1
            """, args, transaction, cancellationToken: ct));
        if (campaign?.Questions is not { Length: > 0 } questions)
            return null;

        var key = new { CampaignId = campaign.Id, leadId };
        var started = await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO campaign_qualifications (campaign_id, lead_id, questions)
            VALUES (@CampaignId, @LeadId, @Questions) ON CONFLICT DO NOTHING
            """, new { CampaignId = campaign.Id, leadId, questions }, transaction, cancellationToken: ct)) == 1;
        var flow = await connection.QuerySingleAsync<Qualification>(new CommandDefinition(
            """
            SELECT questions, answers, offered_slot_ids AS OfferedSlotIds, booked_slot_id AS BookedSlotId
            FROM campaign_qualifications WHERE campaign_id = @CampaignId AND lead_id = @LeadId FOR UPDATE
            """, key, transaction, cancellationToken: ct));
        if (flow.BookedSlotId.HasValue)
        {
            if (campaign.ProposalPlans != "[]" && System.Text.RegularExpressions.Regex.IsMatch(message,
                @"\b(desconto|condição especial|condicao especial|negociar)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                await RequestProposalReviewAsync(connection, transaction, tenantId, leadId, now,
                    "O lead solicitou negociação ou condições especiais pelo Direct.", ct);
                transaction.Commit();
                return "Nossa equipe vai continuar o atendimento e revisar as condições com você.";
            }

            if (!message.Trim().Equals("PROPOSTA", StringComparison.OrdinalIgnoreCase))
                return null;

            var proposalReply = await PrepareProposalAsync(connection, transaction, tenantId, campaign.Id, leadId,
                campaign.ProposalPlans, flow.Answers.FirstOrDefault(), now, ct);
            transaction.Commit();
            return proposalReply;
        }

        async Task RecordActivity(string type, string title, string description)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO lead_activities (tenant_id, lead_id, type, title, description, created_at)
                VALUES (@TenantId, @LeadId, @Type, @Title, @Description, @Now)
                """, new { tenantId, leadId, type, title, description, now }, transaction, cancellationToken: ct));
        }

        string reply;
        if (started)
        {
            reply = flow.Questions[0];
        }
        else if (flow.Answers.Length < flow.Questions.Length)
        {
            var question = flow.Questions[flow.Answers.Length];
            var answers = flow.Answers.Append(message).ToArray();
            await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE campaign_qualifications SET answers = @Answers
                WHERE campaign_id = @CampaignId AND lead_id = @LeadId
                """, new { CampaignId = campaign.Id, leadId, answers }, transaction, cancellationToken: ct));
            await RecordActivity("campaign_qualification", "Qualificação · " + campaign.Name[..Math.Min(campaign.Name.Length, 220)],
                $"{question}\n{message}");
            if (answers.Length == flow.Questions.Length)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    UPDATE leads SET pipeline_stage = 'Interessado', status = 'Interessado', updated_at = @Now
                    WHERE id = @LeadId AND tenant_id = @TenantId
                      AND pipeline_stage IN ('Novo', 'Contato Iniciado', 'Respondeu')
                    """, args, transaction, cancellationToken: ct));
            }
            reply = answers.Length < flow.Questions.Length
                ? flow.Questions[answers.Length]
                : await OfferSlotsAsync(connection, transaction, campaign.Id, tenantId, leadId, now, ct);
        }
        else
        {
            var parts = message.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var slotId = 0L;
            var valid = parts.Length == 2 && parts[0].Equals("AGENDAR", StringComparison.OrdinalIgnoreCase) &&
                long.TryParse(parts[1], out slotId) && flow.OfferedSlotIds.Contains(slotId);
            var booked = valid ? await connection.QuerySingleOrDefaultAsync<DateTime?>(new CommandDefinition(
                """
                UPDATE campaign_demo_slots SET booked_lead_id = @LeadId, booked_at = @Now
                WHERE id = @SlotId AND campaign_id = @CampaignId AND tenant_id = @TenantId
                  AND booked_at IS NULL AND starts_at > @Now
                RETURNING starts_at
                """, new { slotId, CampaignId = campaign.Id, tenantId, leadId, now }, transaction, cancellationToken: ct)) : null;

            if (booked is DateTime startsAt)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    UPDATE campaign_qualifications SET booked_slot_id = @SlotId
                    WHERE campaign_id = @CampaignId AND lead_id = @LeadId
                    """, new { slotId, CampaignId = campaign.Id, leadId }, transaction, cancellationToken: ct));
                reply = $"Demonstração confirmada para {FormatSlot(startsAt)} (horário de Brasília).";
                if (campaign.ProposalPlans != "[]")
                    reply += " Para conhecer as condições comerciais, responda PROPOSTA.";
                await RecordActivity("campaign_demo", "Demonstração agendada", reply);
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    INSERT INTO crm_tasks (tenant_id, lead_id, title, description, due_date)
                    VALUES (@TenantId, @LeadId, 'Realizar demonstração', @Description, @StartsAt)
                    """, new { tenantId, leadId, Description = reply, startsAt }, transaction, cancellationToken: ct));
            }
            else
            {
                reply = await OfferSlotsAsync(connection, transaction, campaign.Id, tenantId, leadId, now, ct);
            }
        }

        transaction.Commit();
        return reply;
    }

    private static async Task<string> OfferSlotsAsync(System.Data.IDbConnection connection, System.Data.IDbTransaction transaction,
        Guid campaignId, Guid tenantId, Guid leadId, DateTime now, CancellationToken ct)
    {
        var slots = (await connection.QueryAsync<CampaignDemoSlot>(new CommandDefinition(
            """
            SELECT id, starts_at AS StartsAt, booked_at AS BookedAt FROM campaign_demo_slots
            WHERE campaign_id = @CampaignId AND tenant_id = @TenantId AND booked_at IS NULL AND starts_at > @Now
            ORDER BY starts_at LIMIT 3
            """, new { campaignId, tenantId, now }, transaction, cancellationToken: ct))).ToArray();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE campaign_qualifications SET offered_slot_ids = @SlotIds
            WHERE campaign_id = @CampaignId AND lead_id = @LeadId
            """, new { campaignId, leadId, SlotIds = slots.Select(slot => slot.Id).ToArray() }, transaction, cancellationToken: ct));
        if (slots.Length == 0)
        {
            var title = "Combinar horário da demonstração";
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO crm_tasks (tenant_id, lead_id, title, description, due_date)
                SELECT @TenantId, @LeadId, @Title, 'Lead qualificado sem horários disponíveis na campanha.', @Now
                WHERE NOT EXISTS (
                    SELECT 1 FROM crm_tasks WHERE tenant_id = @TenantId AND lead_id = @LeadId
                      AND title = @Title AND status = 'Pendente'
                )
                """, new { tenantId, leadId, title, now }, transaction, cancellationToken: ct));
            return "Nossa equipe vai combinar um horário com você.";
        }

        var options = slots.Select(slot => $"AGENDAR {slot.Id} — {FormatSlot(slot.StartsAt)}");
        return "Horários disponíveis (Brasília). Responda com AGENDAR e o código escolhido:\n" + string.Join("\n", options);
    }

    private static string FormatSlot(DateTime startsAt) =>
        TimeZoneInfo.ConvertTimeFromUtc(startsAt, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).ToString("dd/MM/yyyy HH:mm");

    private sealed class SalesCampaign
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string[]? Questions { get; set; }
        public string ProposalPlans { get; set; } = "[]";
    }
    private sealed class Qualification
    {
        public string[] Questions { get; set; } = [];
        public string[] Answers { get; set; } = [];
        public long[] OfferedSlotIds { get; set; } = [];
        public long? BookedSlotId { get; set; }
    }
}

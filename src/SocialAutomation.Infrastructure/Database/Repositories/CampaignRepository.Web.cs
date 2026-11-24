using Dapper;
using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public partial class CampaignRepository
{
    public async Task<bool> SetPublicCaptureAsync(Guid tenantId, Guid id, bool enabled, string? title, string? intro, CancellationToken ct)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.ExecuteAsync(new CommandDefinition("""
            UPDATE campaigns SET public_capture_enabled = @Enabled, updated_at = NOW(),
                public_capture_title = COALESCE(@Title, public_capture_title),
                public_capture_intro = COALESCE(@Intro, public_capture_intro)
            WHERE id = @Id AND tenant_id = @TenantId
              AND (NOT @Enabled OR (capture_material IS NOT NULL AND cardinality(qualification_questions) > 0))
            """, new { tenantId, id, enabled, title, intro }, cancellationToken: ct)) == 1;
    }

    public async Task<IEnumerable<CampaignChannelMetrics>?> GetWebMetricsAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        using var db = _connectionFactory.CreateConnection();
        var args = new { tenantId, id };
        if (!await db.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM campaigns WHERE id = @Id AND tenant_id = @TenantId)", args, cancellationToken: ct)))
            return null;
        return await db.QueryAsync<CampaignChannelMetrics>(new CommandDefinition("""
            SELECT v.channel, count(*)::int AS Visits, count(v.diagnosed_at)::int AS Diagnoses,
                   count(v.booked_slot_id)::int AS Demos,
                   count(*) FILTER (WHERE v.booked_slot_id IS NOT NULL AND p.accepted_at IS NOT NULL)::int AS Accepted
            FROM campaign_web_visits v
            JOIN campaigns c ON c.id = v.campaign_id AND c.tenant_id = @TenantId
            LEFT JOIN campaign_proposals p ON p.campaign_id = c.id AND p.lead_id = v.lead_id AND p.tenant_id = c.tenant_id AND p.web_visitor_id = v.visitor_id
            WHERE c.id = @Id GROUP BY v.channel ORDER BY Visits DESC, v.channel
            """, args, cancellationToken: ct));
    }

    public async Task<PublicCampaignCapture?> VisitCaptureAsync(Guid id, Guid visitor, string channel, CancellationToken ct)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<PublicCampaignCapture>(new CommandDefinition("""
            WITH recorded AS (
                INSERT INTO campaign_web_visits (campaign_id, visitor_id, channel)
                SELECT id, @Visitor, @Channel FROM campaigns WHERE id = @Id AND public_capture_enabled
                ON CONFLICT DO NOTHING
            )
            SELECT COALESCE(c.public_capture_title, c.name) AS Name, t.name AS CompanyName, c.public_capture_intro AS Caption, c.qualification_questions AS Questions
            FROM campaigns c JOIN tenants t ON t.id = c.tenant_id
            WHERE c.id = @Id AND c.public_capture_enabled
            """, new { id, visitor, channel }, cancellationToken: ct));
    }

    public async Task<PublicCampaignDiagnosis?> DiagnoseCaptureAsync(Guid id, Guid visitor, string[] answers,
        PublicCampaignContact? contact, CancellationToken ct)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();
        var args = new { id, visitor };
        var entry = await db.QuerySingleOrDefaultAsync<WebEntry>(new CommandDefinition(WebEntrySql, args, tx, cancellationToken: ct));
        if (entry is null || answers.Length != entry.Questions.Length)
            return null;

        var leadId = entry.LeadId;
        if (contact is not null && leadId is null)
        {
            var tenantId = entry.TenantId;
            // Serialize contact lookup for this company so concurrent submissions cannot create duplicate leads.
            await db.ExecuteAsync(new CommandDefinition(
                "SELECT id FROM tenants WHERE id = @TenantId FOR NO KEY UPDATE", new { tenantId }, tx, cancellationToken: ct));
            var identity = new { tenantId, contact.Name, contact.Email, contact.Phone };
            leadId = await db.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition("""
                SELECT id FROM leads
                WHERE tenant_id = @TenantId AND
                    ((CAST(@Email AS text) IS NOT NULL AND lower(email) = @Email) OR
                     (CAST(@Phone AS text) IS NOT NULL AND regexp_replace(phone, '[^0-9]', '', 'g') = @Phone))
                ORDER BY created_at, id LIMIT 1
                """, identity, tx, cancellationToken: ct));
            leadId ??= await db.QuerySingleAsync<Guid>(new CommandDefinition("""
                INSERT INTO leads (tenant_id, name, email, phone, source, status, pipeline_stage)
                VALUES (@TenantId, @Name, @Email, @Phone, 'campaign_web', 'Interessado', 'Interessado')
                RETURNING id
                """, identity, tx, cancellationToken: ct));

            // Keep the submitted answers and source in the CRM without overwriting Instagram qualification history.
            var description = $"Canal: {entry.Channel}\nContato autorizado pelo formulário.\n" +
                string.Join("\n", entry.Questions.Zip(answers, (question, answer) => $"{question} {answer}"));
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO lead_activities (tenant_id, lead_id, type, title, description)
                VALUES (@TenantId, @LeadId, 'campaign_web', 'Diagnóstico pelo site', @Description);
                INSERT INTO crm_tasks (tenant_id, lead_id, title, description, due_date)
                SELECT @TenantId, @LeadId, 'Retornar diagnóstico da campanha', @Description, NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM crm_tasks WHERE tenant_id = @TenantId AND lead_id = @LeadId
                      AND title = 'Retornar diagnóstico da campanha' AND status = 'Pendente'
                );
                UPDATE campaign_web_visits SET lead_id = @LeadId, contact_consented_at = NOW(), answers = @Answers
                WHERE campaign_id = @Id AND visitor_id = @Visitor;
                """, new { tenantId, leadId, description, answers, id, visitor }, tx, cancellationToken: ct));
        }

        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE campaign_web_visits SET diagnosed_at = COALESCE(diagnosed_at, NOW())
            WHERE campaign_id = @Id AND visitor_id = @Visitor
            """, args, tx, cancellationToken: ct));
        var slots = await db.QueryAsync<PublicCampaignSlot>(new CommandDefinition("""
            SELECT id, starts_at AS StartsAt FROM campaign_demo_slots
            WHERE campaign_id = @Id AND tenant_id = @TenantId AND starts_at > NOW() AND booked_at IS NULL
            ORDER BY starts_at LIMIT 12
            """, new { id, entry.TenantId }, tx, cancellationToken: ct));
        var booked = entry.BookedSlotId is long bookedId ? await db.QuerySingleOrDefaultAsync<PublicCampaignSlot>(
            new CommandDefinition("SELECT id, starts_at AS StartsAt FROM campaign_demo_slots WHERE id = @BookedId",
                new { bookedId }, tx, cancellationToken: ct)) : null;
        tx.Commit();
        return new PublicCampaignDiagnosis(entry.Material, slots, leadId.HasValue, entry.ProposalPlans != "[]", booked);
    }

    public async Task<PublicCampaignSlot?> BookCaptureAsync(Guid id, Guid visitor, long slotId, CancellationToken ct)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();
        var entry = await db.QuerySingleOrDefaultAsync<WebEntry>(new CommandDefinition(
            WebEntrySql, new { id, visitor }, tx, cancellationToken: ct));
        if (entry?.LeadId is not Guid leadId)
            return null;

        var args = new { id, visitor, slotId, leadId, entry.TenantId };
        // Lock the lead as well as the visit to prevent two browser sessions booking twice.
        await db.ExecuteAsync(new CommandDefinition(
            "SELECT id FROM leads WHERE id = @LeadId AND tenant_id = @TenantId FOR NO KEY UPDATE", args, tx, cancellationToken: ct));
        var existing = await db.QueryFirstOrDefaultAsync<PublicCampaignSlot>(new CommandDefinition("""
            SELECT id, starts_at AS StartsAt FROM campaign_demo_slots
            WHERE campaign_id = @Id AND tenant_id = @TenantId AND booked_lead_id = @LeadId AND starts_at > NOW()
            ORDER BY starts_at LIMIT 1
            """, args, tx, cancellationToken: ct));
        if (entry.BookedSlotId is not null || existing is not null)
            return existing?.Id == slotId && entry.BookedSlotId == slotId ? existing : null;

        var slot = await db.QuerySingleOrDefaultAsync<PublicCampaignSlot>(new CommandDefinition("""
            UPDATE campaign_demo_slots SET booked_lead_id = @LeadId, booked_at = NOW()
            WHERE id = @SlotId AND campaign_id = @Id AND tenant_id = @TenantId AND starts_at > NOW() AND booked_at IS NULL
            RETURNING id, starts_at AS StartsAt
            """, args, tx, cancellationToken: ct));
        if (slot is null)
            return null;

        await db.ExecuteAsync(new CommandDefinition("""
            UPDATE campaign_web_visits SET booked_slot_id = @SlotId WHERE campaign_id = @Id AND visitor_id = @Visitor;
            INSERT INTO crm_tasks (tenant_id, lead_id, title, description, due_date)
            VALUES (@TenantId, @LeadId, 'Realizar demonstração', 'Reserva pela página pública da campanha.', @StartsAt);
            INSERT INTO lead_activities (tenant_id, lead_id, type, title, description)
            VALUES (@TenantId, @LeadId, 'campaign_demo', 'Demonstração agendada', 'Reserva pela página pública da campanha.');
            """, new { id, visitor, slotId, leadId, entry.TenantId, slot.StartsAt }, tx, cancellationToken: ct));
        tx.Commit();
        return slot;
    }

    public async Task<string?> ProposeCaptureAsync(Guid id, Guid visitor, CancellationToken ct)
    {
        using var db = _connectionFactory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();
        var entry = await db.QuerySingleOrDefaultAsync<WebEntry>(new CommandDefinition(
            WebEntrySql, new { id, visitor }, tx, cancellationToken: ct));
        if (entry?.LeadId is not Guid leadId || entry.BookedSlotId is null)
            return null;
        var reply = await PrepareProposalAsync(db, tx, entry.TenantId, id, leadId, entry.ProposalPlans,
            entry.Answers?.FirstOrDefault(), DateTime.UtcNow, ct, visitor);
        tx.Commit();
        return reply;
    }

    private const string WebEntrySql = """
        SELECT c.tenant_id AS TenantId, c.capture_material AS Material, c.qualification_questions AS Questions,
               c.proposal_plans::text AS ProposalPlans, v.lead_id AS LeadId, v.booked_slot_id AS BookedSlotId,
               v.channel, v.answers
        FROM campaigns c JOIN campaign_web_visits v ON v.campaign_id = c.id
        WHERE c.id = @Id AND v.visitor_id = @Visitor AND c.public_capture_enabled
        FOR UPDATE OF v
        """;

    private sealed class WebEntry
    {
        public Guid TenantId { get; init; }
        public string Material { get; init; } = "";
        public string[] Questions { get; init; } = [];
        public string ProposalPlans { get; init; } = "[]";
        public Guid? LeadId { get; init; }
        public long? BookedSlotId { get; init; }
        public string Channel { get; init; } = "";
        public string[]? Answers { get; init; }
    }
}

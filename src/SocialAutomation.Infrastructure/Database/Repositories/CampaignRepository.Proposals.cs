using System.Data;
using System.Text.Json;
using Dapper;
using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public partial class CampaignRepository
{
    private readonly string _proposalBaseUrl;

    public async Task<ProposalPlan[]?> GetProposalPlansAsync(Guid tenantId, Guid campaignId, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        var json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT proposal_plans::text FROM campaigns WHERE tenant_id = @TenantId AND id = @CampaignId AND instagram_media_id IS NOT NULL",
            new { tenantId, campaignId }, cancellationToken: ct));
        return json is null ? null : JsonSerializer.Deserialize<ProposalPlan[]>(json);
    }

    public async Task<bool> SaveProposalPlansAsync(Guid tenantId, Guid campaignId, ProposalPlan[] plans, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE campaigns SET proposal_plans = CAST(@Json AS jsonb), updated_at = NOW()
            WHERE tenant_id = @TenantId AND id = @CampaignId AND instagram_media_id IS NOT NULL
              AND (cardinality(qualification_questions) > 0 OR @Disable)
            """, new { tenantId, campaignId, Json = JsonSerializer.Serialize(plans), Disable = plans.Length == 0 },
            cancellationToken: ct)) == 1;
    }

    private async Task<string?> PrepareProposalAsync(IDbConnection db, IDbTransaction tx, Guid tenantId, Guid campaignId,
        Guid leadId, string plansJson, string? vehicleAnswer, DateTime now, CancellationToken ct, Guid? webVisitor = null)
    {
        var plans = JsonSerializer.Deserialize<ProposalPlan[]>(plansJson) ?? [];
        if (plans.Length == 0)
            return null;

        var args = new { tenantId, campaignId, leadId, now };
        await db.ExecuteAsync(new CommandDefinition(
            "SELECT id FROM leads WHERE tenant_id = @TenantId AND id = @LeadId FOR NO KEY UPDATE", args, tx, cancellationToken: ct));
        var existing = await db.QuerySingleOrDefaultAsync<ProposalReference>(new CommandDefinition(
            """
            SELECT id, accepted_at AS AcceptedAt FROM campaign_proposals
            WHERE tenant_id = @TenantId AND campaign_id = @CampaignId AND lead_id = @LeadId
              AND (accepted_at IS NOT NULL OR expires_at > @Now)
            """, args, tx, cancellationToken: ct));
        if (existing is not null)
            return existing.AcceptedAt.HasValue ? "Seu aceite já está registrado. Nossa equipe vai continuar o atendimento." : ProposalLink(existing.Id);

        var match = System.Text.RegularExpressions.Regex.Match(vehicleAnswer?.Trim() ?? "",
            @"^([0-9]{1,6})(?:\s+(?:veículos|veiculos|carros))?[.!]?$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var plan = match.Success && int.TryParse(match.Groups[1].Value, out var vehicles)
            ? plans.FirstOrDefault(item => vehicles <= item.MaxVehicles)
            : null;
        if (plan is null)
        {
            await RequestProposalReviewAsync(db, tx, tenantId, leadId, now,
                "A quantidade informada não se enquadrou nas regras da campanha.", ct, handoffInbox: webVisitor is null);
            return "Vou encaminhar sua proposta para nossa equipe revisar as condições.";
        }

        var proposalId = await db.QuerySingleAsync<Guid>(new CommandDefinition(
            """
            INSERT INTO campaign_proposals (tenant_id, campaign_id, lead_id, company_name, plan_name, price, terms, created_at, expires_at, web_visitor_id)
            SELECT id, @CampaignId, @LeadId, name, @Name, @Price, @Terms, @Now, @Now + INTERVAL '7 days', @WebVisitor
            FROM tenants WHERE id = @TenantId
            ON CONFLICT (campaign_id, lead_id) DO UPDATE SET
                id = gen_random_uuid(), company_name = EXCLUDED.company_name, plan_name = EXCLUDED.plan_name,
                price = EXCLUDED.price, terms = EXCLUDED.terms, created_at = EXCLUDED.created_at, expires_at = EXCLUDED.expires_at, web_visitor_id = EXCLUDED.web_visitor_id
            RETURNING id
            """, new { tenantId, campaignId, leadId, plan.Name, plan.Price, plan.Terms, now, webVisitor }, tx, cancellationToken: ct));
        await db.ExecuteAsync(new CommandDefinition(
            """
            UPDATE leads SET pipeline_stage = 'Proposta', status = 'Proposta', updated_at = @Now
            WHERE tenant_id = @TenantId AND id = @LeadId AND pipeline_stage IN ('Novo', 'Contato Iniciado', 'Respondeu', 'Interessado');
            INSERT INTO lead_activities (tenant_id, lead_id, type, title, description, created_at)
            VALUES (@TenantId, @LeadId, 'campaign_proposal', 'Proposta gerada', @Name, @Now)
            """, new { tenantId, leadId, plan.Name, now }, tx, cancellationToken: ct));
        return ProposalLink(proposalId);
    }

    private static async Task RequestProposalReviewAsync(IDbConnection db, IDbTransaction tx, Guid tenantId,
        Guid leadId, DateTime now, string reason, CancellationToken ct, bool handoffInbox = true)
    {
        await db.ExecuteAsync(new CommandDefinition(
                """
                UPDATE conversations SET mode = 'HUMANO'
                WHERE @HandoffInbox AND tenant_id = @TenantId AND lead_id = @LeadId;
                INSERT INTO crm_tasks (tenant_id, lead_id, title, description, due_date)
                SELECT @TenantId, @LeadId, 'Revisar proposta comercial', @Reason, @Now
                WHERE NOT EXISTS (
                    SELECT 1 FROM crm_tasks WHERE tenant_id = @TenantId AND lead_id = @LeadId
                      AND title = 'Revisar proposta comercial' AND status = 'Pendente'
                )
                """, new { tenantId, leadId, now, reason, handoffInbox }, tx, cancellationToken: ct));
    }

    private string ProposalLink(Guid id) =>
        $"Sua proposta está pronta. Confira o preço e as condições e registre seu aceite: {_proposalBaseUrl.TrimEnd('/')}/api/propostas?codigo={id}";

    public async Task<CampaignProposal?> GetPublicProposalAsync(Guid id, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<CampaignProposal>(new CommandDefinition(
            """
            SELECT company_name AS CompanyName, plan_name AS PlanName, price, terms,
                   expires_at AS ExpiresAt, accepted_at AS AcceptedAt
            FROM campaign_proposals WHERE id = @Id AND (accepted_at IS NOT NULL OR expires_at > NOW())
            """, new { id }, cancellationToken: ct));
    }

    public async Task<bool> AcceptProposalAsync(Guid id, CancellationToken ct)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            WITH accepted AS (
                UPDATE campaign_proposals SET accepted_at = NOW()
                WHERE id = @Id AND accepted_at IS NULL AND expires_at > NOW()
                RETURNING tenant_id, lead_id, plan_name
            ), logged AS (
                INSERT INTO lead_activities (tenant_id, lead_id, type, title, description)
                SELECT tenant_id, lead_id, 'campaign_proposal_accepted', 'Aceite comercial registrado', plan_name FROM accepted
            )
            INSERT INTO crm_tasks (tenant_id, lead_id, title, description, due_date)
            SELECT tenant_id, lead_id, 'Dar continuidade à proposta aceita', plan_name, NOW() FROM accepted
            """, new { id }, cancellationToken: ct));
        return (await GetPublicProposalAsync(id, ct))?.AcceptedAt.HasValue == true;
    }

    private sealed class ProposalReference
    {
        public Guid Id { get; init; }
        public DateTime? AcceptedAt { get; init; }
    }
}

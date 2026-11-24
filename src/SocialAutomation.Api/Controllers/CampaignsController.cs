using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Domain.Interfaces.Repositories;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.Automation.DTOs;
using SocialAutomation.Application.Automation.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/campaigns")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly CampaignService _campaignService;

    public CampaignsController(CampaignService campaignService)
    {
        _campaignService = campaignService;
    }

    [HttpGet]
    [RequirePermission("campaigns.read")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _campaignService.GetAllAsync(ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost]
    [RequirePermission("campaigns.create")]
    public async Task<IActionResult> Create([FromBody] CreateCampaignRequest request, CancellationToken ct)
    {
        var result = await _campaignService.CreateCampaignAsync(request, ct);
        if (!result.Success) return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true, data = new { id = result.Data } });
    }

    [HttpPut("{id:guid}/comment-status")]
    [RequirePermission("campaigns.execute")]
    public async Task<IActionResult> SetCommentStatus(Guid id, [FromQuery] bool enabled, CancellationToken ct)
    {
        var result = await _campaignService.SetCommentCampaignEnabledAsync(id, enabled, ct);
        return result.Success ? Ok(new { success = true }) : BadRequest(new { success = false, error = result.Error });
    }

    [HttpPut("{id:guid}/public-capture")]
    [RequirePermission("campaigns.execute")]
    public async Task<IActionResult> SetPublicCapture(Guid id, [FromBody] PublicCaptureRequest request,
        [FromServices] ITenantContext tenant, [FromServices] ICampaignRepository campaigns, CancellationToken ct)
    {
        var title = request.Title?.Trim();
        var intro = request.Intro?.Trim();
        if (request.Enabled && (title is not { Length: >= 1 and <= 160 } || intro is not { Length: >= 1 and <= 800 }))
            return BadRequest(new { error = "Preencha o título e a descrição da página." });

        return await campaigns.SetPublicCaptureAsync(tenant.TenantId, id, request.Enabled,
            request.Enabled ? title : null, request.Enabled ? intro : null, ct)
            ? Ok(new { success = true })
            : BadRequest(new { error = "A campanha precisa de perguntas e material para publicar a página." });
    }

    public sealed record PublicCaptureRequest(bool Enabled, string? Title, string? Intro);

    [HttpGet("{id:guid}/public-capture/metrics")]
    [RequirePermission("campaigns.read")]
    public async Task<IActionResult> GetWebMetrics(Guid id, [FromServices] ITenantContext tenant,
        [FromServices] ICampaignRepository campaigns, CancellationToken ct)
    {
        var metrics = await campaigns.GetWebMetricsAsync(tenant.TenantId, id, ct);
        return metrics is null ? NotFound() : Ok(new { data = metrics });
    }

    [HttpGet("{id:guid}/sales-flow")]
    [RequirePermission("campaigns.read")]
    public async Task<IActionResult> GetSalesFlow(Guid id, [FromServices] ITenantContext tenant,
        [FromServices] ICampaignRepository campaigns, CancellationToken ct)
    {
        var data = await campaigns.GetSalesSettingsAsync(tenant.TenantId, id, ct);
        return data is null ? NotFound() : Ok(new { success = true, data });
    }

    [HttpPut("{id:guid}/sales-flow")]
    [RequirePermission("campaigns.execute")]
    public async Task<IActionResult> SaveSalesFlow(Guid id, [FromBody] SalesFlowRequest request,
        [FromServices] ITenantContext tenant, [FromServices] ICampaignRepository campaigns, CancellationToken ct)
    {
        if (request.Questions is null || request.NewSlots is null)
            return BadRequest(new { success = false, error = "Informe as perguntas e os novos horários." });

        var questions = request.Questions.Select(question => question?.Trim() ?? "").ToArray();
        var slots = request.NewSlots.Select(slot => slot.UtcDateTime).ToArray();
        if (questions.Length > 5 || questions.Any(question => question.Length is < 1 or > 300) ||
            slots.Length > 20 || slots.Distinct().Count() != slots.Length ||
            slots.Any(slot => slot <= DateTime.UtcNow || slot.Ticks % TimeSpan.FromMinutes(30).Ticks != 0))
            return BadRequest(new { success = false, error = "Use até 5 perguntas e horários futuros distintos em intervalos de 30 minutos." });

        var saved = await campaigns.SaveSalesSettingsAsync(tenant.TenantId, id, questions, slots, ct);
        return saved ? Ok(new { success = true }) :
            Conflict(new { success = false, error = "Campanha indisponível, horário já cadastrado ou oferta de captura sem perguntas." });
    }

    public sealed record SalesFlowRequest(string[] Questions, DateTimeOffset[] NewSlots);

    [HttpGet("{id:guid}/proposal-plans")]
    [RequirePermission("campaigns.read")]
    public async Task<IActionResult> GetPlans(Guid id, [FromServices] ITenantContext tenant,
        [FromServices] ICampaignRepository campaigns, CancellationToken ct)
    {
        var data = await campaigns.GetProposalPlansAsync(tenant.TenantId, id, ct);
        return data is null ? NotFound() : Ok(new { success = true, data });
    }

    [HttpPut("{id:guid}/proposal-plans")]
    [RequirePermission("campaigns.execute")]
    public async Task<IActionResult> SavePlans(Guid id, [FromBody] SocialAutomation.Domain.Entities.ProposalPlan[] plans,
        [FromServices] ITenantContext tenant, [FromServices] ICampaignRepository campaigns, CancellationToken ct)
    {
        if (plans.Length > 10 || plans.Any(plan => plan is null || plan.MaxVehicles is < 1 or > 999999 ||
            string.IsNullOrWhiteSpace(plan.Name) || plan.Name.Length > 100 || plan.Price is <= 0 or > 9999999999.99m ||
            decimal.Round(plan.Price, 2) != plan.Price || string.IsNullOrWhiteSpace(plan.Terms) || plan.Terms.Length > 4000) ||
            plans.Select(plan => plan.MaxVehicles).Distinct().Count() != plans.Length)
            return BadRequest(new { success = false, error = "Preencha faixas distintas, preço positivo e as condições de cada plano." });

        var ordered = plans.OrderBy(plan => plan.MaxVehicles).Select(plan =>
            plan with { Name = plan.Name.Trim(), Terms = plan.Terms.Trim() }).ToArray();
        var saved = await campaigns.SaveProposalPlansAsync(tenant.TenantId, id, ordered, ct);
        return saved ? Ok(new { success = true }) :
            BadRequest(new { success = false, error = "Salve a qualificação da campanha antes de cadastrar os planos." });
    }
}

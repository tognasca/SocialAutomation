using SocialAutomation.Application.Automation.DTOs;
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;
using System.Text.Json;

namespace SocialAutomation.Application.Automation.Services;

public class CampaignService
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IInstagramAccountRepository _accounts;
    private readonly ILeadRepository _leadRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CampaignService(
        ICampaignRepository campaignRepository,
        ILeadRepository leadRepository, IInstagramAccountRepository accounts,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _campaignRepository = campaignRepository;
        _leadRepository = leadRepository;
        _accounts = accounts;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IEnumerable<CampaignDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var campaigns = await _campaignRepository.GetAllByTenantAsync(_tenantContext.TenantId, ct);
        return Result.Ok(campaigns.Select(c => new CampaignDto
        {
            Id = c.Id,
            Name = c.Name,
            InstagramMediaId = c.InstagramMediaId,
            CommentKeyword = c.CommentKeyword,
            MessageTemplate = c.MessageTemplate,
            Status = c.Status,
            TotalAudience = c.TotalAudience,
            SentCount = c.SentCount,
            ReplyCount = c.ReplyCount,
            PublicCaptureTitle = c.PublicCaptureTitle,
            PublicCaptureIntro = c.PublicCaptureIntro,
            PublicCaptureEnabled = c.PublicCaptureEnabled,
            ReelScript = c.ReelScript,
            SocialCaption = c.SocialCaption,
            CaptureMaterial = c.CaptureMaterial,
            QualifiedCount = c.QualifiedCount,
            BookedCount = c.BookedCount,
            AcceptedCount = c.AcceptedCount,
            FailedCount = c.FailedCount,
            CreatedAt = c.CreatedAt
        }));
    }

    public async Task<Result<Guid>> CreateCampaignAsync(CreateCampaignRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.MessageTemplate))
            return Result.Fail<Guid>("Nome e mensagem da campanha são obrigatórios.");

        if (!string.IsNullOrWhiteSpace(request.InstagramMediaId) || !string.IsNullOrWhiteSpace(request.CommentKeyword))
            return await CreateCommentCampaignAsync(request, ct);

        var (leads, total) = await _leadRepository.GetPagedAsync(_tenantContext.TenantId, 1, 1000, null, request.StatusFilter, null, request.TagIds.FirstOrDefault(), null, ct);

        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            InstagramAccountId = request.InstagramAccountId,
            Name = request.Name.Trim(),
            MessageTemplate = request.MessageTemplate.Trim(),
            SegmentFilter = JsonSerializer.Serialize(new { request.TagIds, request.StatusFilter }),
            Status = "Agendada",
            ScheduledAt = _dateTimeProvider.UtcNow,
            TotalAudience = total,
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow
        };

        await _campaignRepository.CreateAsync(campaign, ct);

        var recipients = leads.Select(l => new CampaignRecipient
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            CampaignId = campaign.Id,
            LeadId = l.Id,
            Status = "Pendente"
        });

        await _campaignRepository.CreateRecipientsAsync(recipients, ct);
        return Result.Ok(campaign.Id);
    }

    public async Task<Result> SetCommentCampaignEnabledAsync(Guid id, bool enabled, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        var campaign = await _campaignRepository.GetByIdAsync(id, tenantId, ct);
        if (campaign?.InstagramMediaId is null)
            return Result.Fail("Campanha de comentários não encontrada.");

        if (enabled)
        {
            var account = campaign.InstagramAccountId is Guid accountId ? await _accounts.GetByIdAsync(accountId, tenantId, ct) : null;
            if (account?.Status != SocialAutomation.Domain.Enums.InstagramAccountStatus.Active)
                return Result.Fail("Conecte a conta do Instagram antes de ativar.");

            var active = await _campaignRepository.GetActiveCommentCampaignsAsync(tenantId, account.Id, campaign.InstagramMediaId, ct);
            if (active.Any(c => c.Id != id && c.CommentKeyword == campaign.CommentKeyword))
                return Result.Fail("Já há uma campanha ativa com essa palavra-chave na publicação.");
        }
        await _campaignRepository.UpdateStatusAsync(id, tenantId, enabled ? "Ativa" : "Pausada", ct);
        return Result.Ok();
    }

    private async Task<Result<Guid>> CreateCommentCampaignAsync(CreateCampaignRequest request, CancellationToken ct)
    {
        var mediaId = request.InstagramMediaId?.Trim() ?? "";
        var keyword = request.CommentKeyword?.Trim().Normalize().ToLowerInvariant() ?? "";
        if (!System.Text.RegularExpressions.Regex.IsMatch(mediaId, @"^[0-9]{1,40}$") ||
            !System.Text.RegularExpressions.Regex.IsMatch(keyword, @"^[\p{L}\p{M}\p{N}]{1,50}$"))
            return Result.Fail<Guid>("Informe o ID numérico da publicação e uma palavra-chave de até 50 caracteres.");

        if (request.Name.Trim().Length > 255 || request.MessageTemplate.Trim().Length > 1000)
            return Result.Fail<Guid>("Use até 255 caracteres no nome e 1.000 na mensagem.");

        var script = request.ReelScript?.Trim();
        var caption = request.SocialCaption?.Trim();
        var material = request.CaptureMaterial?.Trim();
        var questions = request.CaptureQuestions?.Select(question => question?.Trim() ?? "").ToArray();
        if (script?.Length > 4000 || caption?.Length > 2200 || material?.Length > 800 ||
            (questions is not null && (questions.Length > 5 || questions.Any(question => question.Length is < 1 or > 300))))
            return Result.Fail<Guid>("Use até 4.000 caracteres no roteiro, 2.200 na legenda, 800 no material e 5 perguntas de até 300 caracteres.");

        if (!string.IsNullOrEmpty(material) && questions is not { Length: > 0 })
            return Result.Fail<Guid>("Informe pelo menos uma pergunta para liberar o material no Direct.");

        var tenantId = _tenantContext.TenantId;
        var account = request.InstagramAccountId is Guid accountId ? await _accounts.GetByIdAsync(accountId, tenantId, ct) : null;
        if (account?.Status != SocialAutomation.Domain.Enums.InstagramAccountStatus.Active)
            return Result.Fail<Guid>("Selecione uma conta do Instagram ativa da sua empresa.");

        var now = _dateTimeProvider.UtcNow;
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(), TenantId = tenantId, InstagramAccountId = account.Id,
            Name = request.Name.Trim(), MessageTemplate = request.MessageTemplate.Trim(),
            ReelScript = script, SocialCaption = caption,
            CaptureMaterial = string.IsNullOrEmpty(material) ? null : material,
            QualificationQuestions = questions,
            InstagramMediaId = mediaId, CommentKeyword = keyword, Status = "Pausada",
            CreatedAt = now, UpdatedAt = now
        };
        await _campaignRepository.CreateAsync(campaign, ct);
        return Result.Ok(campaign.Id);
    }
}

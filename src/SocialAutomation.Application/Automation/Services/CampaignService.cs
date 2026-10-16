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
    private readonly ILeadRepository _leadRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CampaignService(
        ICampaignRepository campaignRepository,
        ILeadRepository leadRepository,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _campaignRepository = campaignRepository;
        _leadRepository = leadRepository;
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
            MessageTemplate = c.MessageTemplate,
            Status = c.Status,
            TotalAudience = c.TotalAudience,
            SentCount = c.SentCount,
            FailedCount = c.FailedCount,
            CreatedAt = c.CreatedAt
        }));
    }

    public async Task<Result<Guid>> CreateCampaignAsync(CreateCampaignRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.MessageTemplate))
            return Result.Fail<Guid>("Nome e mensagem da campanha são obrigatórios.");

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
}
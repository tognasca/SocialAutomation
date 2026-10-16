
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Application.CRM.DTOs;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Application.CRM.Services;

public class PipelineService
{
    private readonly ILeadRepository _leadRepository;
    private readonly ITenantContext _tenantContext;

    private static readonly string[] DefaultStages = new[]
    {
        "Novo",
        "Contato Iniciado",
        "Respondeu",
        "Interessado",
        "Proposta",
        "Negociação",
        "Cliente"
    };

    public PipelineService(ILeadRepository leadRepository, ITenantContext tenantContext)
    {
        _leadRepository = leadRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<PipelineColumnDto>>> GetBoardAsync(CancellationToken ct = default)
    {
        var columns = new List<PipelineColumnDto>();

        foreach (var stage in DefaultStages)
        {
            var leads = (await _leadRepository.GetByPipelineStageAsync(_tenantContext.TenantId, stage, ct)).ToList();

            columns.Add(new PipelineColumnDto
            {
                Stage = stage,
                Count = leads.Count,
                TotalValue = leads.Sum(x => x.DealValue),
                Leads = leads.Select(l => new LeadResponse
                {
                    Id = l.Id,
                    Name = l.Name,
                    InstagramUsername = l.InstagramUsername,
                    ProfileImage = l.ProfileImage,
                    DealValue = l.DealValue,
                    Score = l.Score,
                    ScoreClassification = l.ScoreClassification,
                    PipelineStage = l.PipelineStage,
                    OwnerName = l.OwnerName,
                    LastInteractionAt = l.LastInteractionAt,
                    NextFollowUpAt = l.NextFollowUpAt,
                    Tags = l.Tags.Select(t => new TagDto { Id = t.Id, Name = t.Name, Color = t.Color }).ToList()
                }).ToList()
            });
        }

        return Result.Ok(columns);
    }
}
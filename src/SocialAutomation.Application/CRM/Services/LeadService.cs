// src/SocialAutomation.Application/CRM/Services/LeadService.cs

using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Application.CRM.DTOs;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.CRM.Services;

public class LeadService
{
    private readonly ILeadRepository _leadRepository;
    private readonly ITagRepository _tagRepository;
    private readonly ILeadActivityRepository _activityRepository;
    private readonly ILeadScoreRuleRepository _scoreRuleRepository;
    private readonly IInboxRepository _inboxRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LeadService(
        ILeadRepository leadRepository,
        ITagRepository tagRepository,
        ILeadActivityRepository activityRepository,
        ILeadScoreRuleRepository scoreRuleRepository,
        IInboxRepository inboxRepository,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _leadRepository = leadRepository;
        _tagRepository = tagRepository;
        _activityRepository = activityRepository;
        _scoreRuleRepository = scoreRuleRepository;
        _inboxRepository = inboxRepository;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PagedResult<LeadResponse>>> GetPagedAsync(
        PagedRequest request, string? status = null, string? stage = null, Guid? tagId = null, Guid? ownerUserId = null, CancellationToken ct = default)
    {
        var (items, total) = await _leadRepository.GetPagedAsync(
            _tenantContext.TenantId, request.Page, request.PageSize, request.Search, status, stage, tagId, ownerUserId, ct);

        var list = items.Select(MapToResponse).ToList();

        return Result.Ok(new PagedResult<LeadResponse>
        {
            Items = list,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }

    public async Task<Result<LeadResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id, _tenantContext.TenantId, ct);
        if (lead == null)
            return Result.Fail<LeadResponse>("Lead não encontrado.");

        return Result.Ok(MapToResponse(lead));
    }

    public async Task<Result<Guid>> CreateAsync(CreateLeadRequest request, CancellationToken ct = default)
    {
        var stage = string.IsNullOrWhiteSpace(request.PipelineStage) ? "Novo" : request.PipelineStage;
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = request.Name.Trim(),
            InstagramUsername = request.InstagramUsername?.Trim().TrimStart('@'),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            Company = request.Company?.Trim(),
            Source = request.Source ?? "Manual",
            Status = stage,
            PipelineStage = stage,
            DealValue = request.DealValue,
            OwnerUserId = request.OwnerUserId,
            Score = 0,
            ScoreClassification = "Frio",
            Notes = request.Notes,
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow
        };

        await _leadRepository.CreateAsync(lead, ct);

        // Associar Tags
        foreach (var tagId in request.TagIds)
        {
            await _tagRepository.AssignTagToLeadAsync(lead.Id, tagId, ct);
        }

        // Criar Atividade Inicial
        await _activityRepository.CreateAsync(new LeadActivity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            LeadId = lead.Id,
            UserId = _tenantContext.UserId,
            Type = "lead_created",
            Title = "Lead Criado",
            Description = $"Lead cadastrado via {lead.Source}",
            CreatedAt = _dateTimeProvider.UtcNow
        }, ct);

        // CRIAR CONVERSA AUTOMÁTICA NO INBOX PARA O LEAD
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            LeadId = lead.Id,
            Mode = "HUMANO",
            Status = "Aberto",
            UnreadCount = 1,
            LastMessagePreview = "Lead cadastrado no sistema.",
            LastMessageAt = _dateTimeProvider.UtcNow,
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow
        };
        await _inboxRepository.CreateConversationAsync(conversation, ct);

        // Criar mensagem inicial do sistema na conversa
        await _inboxRepository.CreateMessageAsync(new Message
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            ConversationId = conversation.Id,
            SenderType = "System",
            Content = $"Conversa iniciada para o lead {lead.Name}.",
            Status = "Sent",
            CreatedAt = _dateTimeProvider.UtcNow
        }, ct);

        return Result.Ok(lead.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateLeadRequest request, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id, _tenantContext.TenantId, ct);
        if (lead == null)
            return Result.Fail("Lead não encontrado.");

        lead.Name = request.Name.Trim();
        lead.InstagramUsername = request.InstagramUsername?.Trim().TrimStart('@');
        lead.Phone = request.Phone?.Trim();
        lead.Email = request.Email?.Trim().ToLowerInvariant();
        lead.Company = request.Company?.Trim();
        lead.Status = request.Status;
        lead.PipelineStage = request.PipelineStage;
        lead.DealValue = request.DealValue;
        lead.OwnerUserId = request.OwnerUserId;
        lead.Notes = request.Notes;
        lead.UpdatedAt = _dateTimeProvider.UtcNow;

        await _leadRepository.UpdateAsync(lead, ct);
        return Result.Ok();
    }

    public async Task<Result> MoveStageAsync(Guid id, MoveLeadStageRequest request, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(id, _tenantContext.TenantId, ct);
        if (lead == null)
            return Result.Fail("Lead não encontrado.");

        var oldStage = lead.PipelineStage;
        await _leadRepository.UpdatePipelineStageAsync(id, _tenantContext.TenantId, request.Stage, request.Order, ct);

        await _activityRepository.CreateAsync(new LeadActivity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            LeadId = lead.Id,
            UserId = _tenantContext.UserId,
            Type = "status_change",
            Title = "Estágio Alterado",
            Description = $"Lead movido de '{oldStage}' para '{request.Stage}'",
            CreatedAt = _dateTimeProvider.UtcNow
        }, ct);

        return Result.Ok();
    }

    public async Task<Result> AddScorePointsAsync(Guid leadId, string eventTrigger, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId, _tenantContext.TenantId, ct);
        if (lead == null) return Result.Fail("Lead não encontrado.");

        var points = await _scoreRuleRepository.GetPointsForTriggerAsync(_tenantContext.TenantId, eventTrigger, ct);
        if (points == 0) points = 5;

        var newScore = lead.Score + points;
        var newClass = LeadScoreCalculator.ClassifyScore(newScore);

        await _leadRepository.UpdateScoreAsync(leadId, _tenantContext.TenantId, newScore, newClass, ct);

        await _activityRepository.CreateAsync(new LeadActivity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            LeadId = lead.Id,
            Type = "score_updated",
            Title = "Pontuação Atualizada",
            Description = $"+{points} pontos por '{eventTrigger}'. Total: {newScore} ({newClass})",
            CreatedAt = _dateTimeProvider.UtcNow
        }, ct);

        return Result.Ok();
    }

    public async Task<Result<IEnumerable<LeadActivityResponse>>> GetActivitiesAsync(Guid leadId, CancellationToken ct = default)
    {
        var activities = await _activityRepository.GetByLeadIdAsync(leadId, _tenantContext.TenantId, ct);
        var res = activities.Select(a => new LeadActivityResponse
        {
            Id = a.Id,
            LeadId = a.LeadId,
            UserName = a.UserName ?? "Sistema",
            Type = a.Type,
            Title = a.Title,
            Description = a.Description,
            CreatedAt = a.CreatedAt
        });

        return Result.Ok(res);
    }

    private static LeadResponse MapToResponse(Lead l) => new()
    {
        Id = l.Id,
        Name = l.Name,
        InstagramUsername = l.InstagramUsername,
        ProfileImage = l.ProfileImage,
        Phone = l.Phone,
        Email = l.Email,
        Company = l.Company,
        Source = l.Source,
        Status = l.Status,
        PipelineStage = l.PipelineStage,
        DealValue = l.DealValue,
        OwnerUserId = l.OwnerUserId,
        OwnerName = l.OwnerName,
        Score = l.Score,
        ScoreClassification = l.ScoreClassification,
        Notes = l.Notes,
        LastInteractionAt = l.LastInteractionAt,
        NextFollowUpAt = l.NextFollowUpAt,
        CreatedAt = l.CreatedAt,
        Tags = l.Tags.Select(t => new TagDto { Id = t.Id, Name = t.Name, Color = t.Color }).ToList()
    };
}
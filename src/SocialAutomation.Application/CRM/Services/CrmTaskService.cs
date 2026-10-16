
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Application.CRM.DTOs;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.CRM.Services;

public class CrmTaskService
{
    private readonly ICrmTaskRepository _taskRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadActivityRepository _activityRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CrmTaskService(
        ICrmTaskRepository taskRepository,
        ILeadRepository leadRepository,
        ILeadActivityRepository activityRepository,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _taskRepository = taskRepository;
        _leadRepository = leadRepository;
        _activityRepository = activityRepository;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PagedResult<TaskResponse>>> GetPagedAsync(
        PagedRequest request, string? status = null, Guid? assignedUserId = null, CancellationToken ct = default)
    {
        var (items, total) = await _taskRepository.GetPagedAsync(
            _tenantContext.TenantId, request.Page, request.PageSize, status, assignedUserId, ct);

        var dtos = items.Select(t => new TaskResponse
        {
            Id = t.Id,
            LeadId = t.LeadId,
            LeadName = t.LeadName,
            AssignedUserId = t.AssignedUserId,
            AssignedUserName = t.AssignedUserName,
            Title = t.Title,
            Description = t.Description,
            DueDate = t.DueDate,
            Status = t.Status,
            Priority = t.Priority,
            CompletedAt = t.CompletedAt,
            CreatedAt = t.CreatedAt
        });

        return Result.Ok(new PagedResult<TaskResponse>
        {
            Items = dtos,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }

    public async Task<Result<Guid>> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(request.LeadId, _tenantContext.TenantId, ct);
        if (lead == null)
            return Result.Fail<Guid>("Lead não encontrado.");

        var task = new CrmTask
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            LeadId = request.LeadId,
            AssignedUserId = request.AssignedUserId ?? _tenantContext.UserId,
            Title = request.Title.Trim(),
            Description = request.Description,
            DueDate = request.DueDate,
            Status = "Pendente",
            Priority = request.Priority ?? "Media",
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow
        };

        await _taskRepository.CreateAsync(task, ct);

        // Atualizar data do próximo follow-up no lead
        if (!lead.NextFollowUpAt.HasValue || task.DueDate < lead.NextFollowUpAt.Value)
        {
            await _leadRepository.UpdateNextFollowUpAsync(lead.Id, _tenantContext.TenantId, task.DueDate, ct);
        }

        await _activityRepository.CreateAsync(new LeadActivity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            LeadId = lead.Id,
            UserId = _tenantContext.UserId,
            Type = "task_created",
            Title = "Follow-up Agendado",
            Description = $"Tarefa: '{task.Title}' para {task.DueDate:dd/MM/yyyy HH:mm}",
            CreatedAt = _dateTimeProvider.UtcNow
        }, ct);

        return Result.Ok(task.Id);
    }

    public async Task<Result> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, _tenantContext.TenantId, ct);
        if (task == null)
            return Result.Fail("Tarefa não encontrada.");

        await _taskRepository.CompleteTaskAsync(id, _tenantContext.TenantId, ct);

        await _activityRepository.CreateAsync(new LeadActivity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            LeadId = task.LeadId,
            UserId = _tenantContext.UserId,
            Type = "task_completed",
            Title = "Follow-up Concluído",
            Description = $"Tarefa concluída: '{task.Title}'",
            CreatedAt = _dateTimeProvider.UtcNow
        }, ct);

        return Result.Ok();
    }
}
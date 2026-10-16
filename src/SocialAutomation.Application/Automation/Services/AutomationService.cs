
using SocialAutomation.Application.Automation.DTOs;
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.Automation.Services;

public class AutomationService
{
    private readonly IAutomationRepository _automationRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AutomationService(
        IAutomationRepository automationRepository,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _automationRepository = automationRepository;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IEnumerable<AutomationFlowDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var flows = await _automationRepository.GetAllByTenantAsync(_tenantContext.TenantId, ct);
        return Result.Ok(flows.Select(f => new AutomationFlowDto
        {
            Id = f.Id,
            Name = f.Name,
            Description = f.Description,
            TriggerType = f.TriggerType,
            IsActive = f.IsActive,
            ExecutionCount = f.ExecutionCount,
            CreatedAt = f.CreatedAt
        }));
    }

    public async Task<Result<Guid>> CreateFlowAsync(CreateAutomationFlowRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Fail<Guid>("Nome do fluxo é obrigatório.");

        var flow = new AutomationFlow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = request.Name.Trim(),
            Description = request.Description,
            TriggerType = request.TriggerType,
            IsActive = request.IsActive,
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow
        };

        await _automationRepository.CreateFlowAsync(flow, ct);

        var nodes = request.Nodes.Select(n => new AutomationNode
        {
            StepOrder = n.StepOrder,
            NodeType = n.NodeType,
            ActionOrCondition = n.ActionOrCondition,
            Config = n.ConfigJson
        });

        await _automationRepository.SaveNodesAsync(flow.Id, _tenantContext.TenantId, nodes, ct);
        return Result.Ok(flow.Id);
    }

    public async Task<Result> ToggleActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        await _automationRepository.ToggleActiveAsync(id, _tenantContext.TenantId, isActive, ct);
        return Result.Ok();
    }
}
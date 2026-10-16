using Microsoft.Extensions.Logging;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;
using System.Text.Json;

namespace SocialAutomation.Application.Automation.Services;

public class AutomationEngine
{
    private readonly IAutomationRepository _automationRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IInboxRepository _inboxRepository;
    private readonly ICrmTaskRepository _taskRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AutomationEngine> _logger;

    public AutomationEngine(
        IAutomationRepository automationRepository,
        ILeadRepository leadRepository,
        ITagRepository tagRepository,
        IInboxRepository inboxRepository,
        ICrmTaskRepository taskRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<AutomationEngine> logger)
    {
        _automationRepository = automationRepository;
        _leadRepository = leadRepository;
        _tagRepository = tagRepository;
        _inboxRepository = inboxRepository;
        _taskRepository = taskRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task TriggerEventAsync(Guid tenantId, Guid leadId, string triggerType, CancellationToken ct = default)
    {
        var flows = await _automationRepository.GetActiveByTriggerAsync(tenantId, triggerType, ct);

        foreach (var flow in flows)
        {
            _logger.LogInformation("Disparando fluxo de automação {FlowName} para o lead {LeadId}", flow.Name, leadId);

            var execution = new AutomationExecution
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FlowId = flow.Id,
                LeadId = leadId,
                CurrentStep = 0,
                Status = "Running",
                CreatedAt = _dateTimeProvider.UtcNow,
                UpdatedAt = _dateTimeProvider.UtcNow
            };

            await _automationRepository.CreateExecutionAsync(execution, ct);
            await ProcessNextStepsAsync(execution, flow, ct);
        }
    }

    public async Task ProcessNextStepsAsync(AutomationExecution execution, AutomationFlow flow, CancellationToken ct = default)
    {
        var lead = await _leadRepository.GetByIdAsync(execution.LeadId, execution.TenantId, ct);
        if (lead == null) return;

        var nodes = flow.Nodes.OrderBy(n => n.StepOrder).ToList();

        for (int i = execution.CurrentStep; i < nodes.Count; i++)
        {
            var node = nodes[i];
            execution.CurrentStep = i + 1;

            if (node.NodeType == "Condition")
            {
                var passed = EvaluateCondition(node, lead);
                if (!passed)
                {
                    _logger.LogInformation("Condição {Condition} falhou para o lead {LeadId}. Interrompendo fluxo.", node.ActionOrCondition, lead.Id);
                    execution.Status = "Completed";
                    await _automationRepository.UpdateExecutionAsync(execution, ct);
                    return;
                }
            }
            else if (node.NodeType == "Delay")
            {
                var delayMinutes = ExtractDelayMinutes(node.Config);
                execution.Status = "WaitingDelay";
                execution.NextExecutionAt = _dateTimeProvider.UtcNow.AddMinutes(delayMinutes);
                await _automationRepository.UpdateExecutionAsync(execution, ct);
                _logger.LogInformation("Fluxo em pausa por {Minutes} minutos para o lead {LeadId}", delayMinutes, lead.Id);
                return;
            }
            else if (node.NodeType == "Action")
            {
                await ExecuteActionAsync(node, lead, execution.TenantId, ct);
            }
        }

        execution.Status = "Completed";
        await _automationRepository.UpdateExecutionAsync(execution, ct);
    }

    private static bool EvaluateCondition(AutomationNode node, Lead lead)
    {
        using var doc = JsonDocument.Parse(node.Config);
        var root = doc.RootElement;

        return node.ActionOrCondition switch
        {
            "ScoreMaiorQue" => lead.Score >= (root.TryGetProperty("minScore", out var s) ? s.GetInt32() : 0),
            "StatusIgual" => lead.Status.Equals(root.TryGetProperty("status", out var st) ? st.GetString() : "", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private async Task ExecuteActionAsync(AutomationNode node, Lead lead, Guid tenantId, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(node.Config);
        var root = doc.RootElement;

        switch (node.ActionOrCondition)
        {
            case "AdicionarTag":
                if (root.TryGetProperty("tagId", out var tagIdEl) && Guid.TryParse(tagIdEl.GetString(), out var tagId))
                {
                    await _tagRepository.AssignTagToLeadAsync(lead.Id, tagId, ct);
                }
                break;

            case "AlterarStatus":
                if (root.TryGetProperty("newStatus", out var statusEl))
                {
                    var newStatus = statusEl.GetString() ?? "Contato Iniciado";
                    await _leadRepository.UpdatePipelineStageAsync(lead.Id, tenantId, newStatus, 0, ct);
                }
                break;

            case "CriarTarefaFollowUp":
                await _taskRepository.CreateAsync(new CrmTask
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    LeadId = lead.Id,
                    Title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "Follow-up Automático" : "Follow-up Automático",
                    DueDate = _dateTimeProvider.UtcNow.AddDays(1),
                    Status = "Pendente",
                    Priority = "Media",
                    CreatedAt = _dateTimeProvider.UtcNow,
                    UpdatedAt = _dateTimeProvider.UtcNow
                }, ct);
                break;

            case "EncaminharParaIA":
                var conversation = await _inboxRepository.GetConversationByLeadIdAsync(lead.Id, tenantId, ct);
                if (conversation != null)
                {
                    await _inboxRepository.UpdateConversationModeAsync(conversation.Id, tenantId, "IA_ATIVA", ct);
                }
                break;
        }
    }

    private static int ExtractDelayMinutes(string configJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(configJson);
            if (doc.RootElement.TryGetProperty("delayMinutes", out var m)) return m.GetInt32();
        }
        catch { }
        return 60; // Default 1h
    }
}
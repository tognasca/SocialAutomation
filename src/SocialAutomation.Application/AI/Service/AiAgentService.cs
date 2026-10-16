using SocialAutomation.Application.AI.Dtos;
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.AI.Services;

public class AiAgentService
{
    private readonly IAiAgentRepository _agentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AiAgentService(
        IAiAgentRepository agentRepository,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _agentRepository = agentRepository;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IEnumerable<AiAgentDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var agents = await _agentRepository.GetAllByTenantAsync(_tenantContext.TenantId, ct);
        return Result.Ok(agents.Select(Map));
    }

    public async Task<Result<Guid>> CreateAsync(UpsertAiAgentRequest request, CancellationToken ct = default)
    {
        var agent = new AiAgent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
            IsDefault = request.IsDefault,
            Personality = request.Personality,
            Tone = request.Tone,
            Objective = request.Objective,
            CompanyName = request.CompanyName,
            CompanyDescription = request.CompanyDescription,
            ProductsServices = request.ProductsServices,
            PricingInfo = request.PricingInfo,
            BusinessHours = request.BusinessHours,
            Faq = request.Faq,
            Instructions = request.Instructions,
            HandoffRules = request.HandoffRules,
            ModelProvider = request.ModelProvider,
            ModelName = request.ModelName,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow
        };

        await _agentRepository.CreateAsync(agent, ct);
        if (request.IsDefault)
            await _agentRepository.SetDefaultAsync(agent.Id, _tenantContext.TenantId, ct);

        return Result.Ok(agent.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpsertAiAgentRequest request, CancellationToken ct = default)
    {
        var agent = await _agentRepository.GetByIdAsync(id, _tenantContext.TenantId, ct);
        if (agent == null) return Result.Fail("Agente não encontrado.");

        agent.Name = request.Name.Trim();
        agent.IsActive = request.IsActive;
        agent.Personality = request.Personality;
        agent.Tone = request.Tone;
        agent.Objective = request.Objective;
        agent.CompanyName = request.CompanyName;
        agent.CompanyDescription = request.CompanyDescription;
        agent.ProductsServices = request.ProductsServices;
        agent.PricingInfo = request.PricingInfo;
        agent.BusinessHours = request.BusinessHours;
        agent.Faq = request.Faq;
        agent.Instructions = request.Instructions;
        agent.HandoffRules = request.HandoffRules;
        agent.ModelProvider = request.ModelProvider;
        agent.ModelName = request.ModelName;
        agent.Temperature = request.Temperature;
        agent.MaxTokens = request.MaxTokens;
        agent.UpdatedAt = _dateTimeProvider.UtcNow;

        await _agentRepository.UpdateAsync(agent, ct);
        if (request.IsDefault)
            await _agentRepository.SetDefaultAsync(id, _tenantContext.TenantId, ct);

        return Result.Ok();
    }

    private static AiAgentDto Map(AiAgent a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        IsActive = a.IsActive,
        IsDefault = a.IsDefault,
        Personality = a.Personality,
        Tone = a.Tone,
        Objective = a.Objective,
        CompanyName = a.CompanyName,
        CompanyDescription = a.CompanyDescription,
        ProductsServices = a.ProductsServices,
        PricingInfo = a.PricingInfo,
        BusinessHours = a.BusinessHours,
        Faq = a.Faq,
        Instructions = a.Instructions,
        HandoffRules = a.HandoffRules,
        ModelProvider = a.ModelProvider,
        ModelName = a.ModelName,
        Temperature = a.Temperature,
        MaxTokens = a.MaxTokens
    };
}
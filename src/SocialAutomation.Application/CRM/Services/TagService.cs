using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Application.CRM.DTOs;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.CRM.Services;

public class TagService
{
    private readonly ITagRepository _tagRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TagService(ITagRepository tagRepository, ITenantContext tenantContext, IDateTimeProvider dateTimeProvider)
    {
        _tagRepository = tagRepository;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IEnumerable<TagDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var tags = await _tagRepository.GetAllByTenantAsync(_tenantContext.TenantId, ct);
        return Result.Ok(tags.Select(t => new TagDto { Id = t.Id, Name = t.Name, Color = t.Color }));
    }

    public async Task<Result<TagDto>> CreateAsync(string name, string color, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Fail<TagDto>("Nome da tag é obrigatório.");

        var existing = await _tagRepository.GetByNameAsync(name, _tenantContext.TenantId, ct);
        if (existing != null)
            return Result.Ok(new TagDto { Id = existing.Id, Name = existing.Name, Color = existing.Color });

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = name.Trim(),
            Color = string.IsNullOrWhiteSpace(color) ? "#6366F1" : color,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        await _tagRepository.CreateAsync(tag, ct);
        return Result.Ok(new TagDto { Id = tag.Id, Name = tag.Name, Color = tag.Color });
    }

    public async Task<Result> AssignToLeadAsync(Guid leadId, Guid tagId, CancellationToken ct = default)
    {
        await _tagRepository.AssignTagToLeadAsync(leadId, tagId, ct);
        return Result.Ok();
    }

    public async Task<Result> RemoveFromLeadAsync(Guid leadId, Guid tagId, CancellationToken ct = default)
    {
        await _tagRepository.RemoveTagFromLeadAsync(leadId, tagId, ct);
        return Result.Ok();
    }
}
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Application.Content.DTOs;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.Content.Services;

public class ContentService
{
    private readonly IContentRepository _contentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ContentService(
        IContentRepository contentRepository,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _contentRepository = contentRepository;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IEnumerable<ContentItemDto>>> GetAllAsync(string? type = null, string? status = null, CancellationToken ct = default)
    {
        var items = await _contentRepository.GetAllByTenantAsync(_tenantContext.TenantId, type, status, ct);
        return Result.Ok(items.Select(Map));
    }

    public async Task<Result<ContentItemDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _contentRepository.GetByIdAsync(id, _tenantContext.TenantId, ct);
        if (item == null) return Result.Fail<ContentItemDto>("Conteúdo não encontrado.");

        return Result.Ok(Map(item));
    }

    public async Task<Result<Guid>> CreateAsync(CreateContentItemRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Fail<Guid>("Título é obrigatório.");

        var status = request.ScheduledAt.HasValue ? "Agendado" : "Rascunho";

        var item = new ContentItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            InstagramAccountId = request.InstagramAccountId,
            Type = request.Type ?? "Post",
            Title = request.Title.Trim(),
            Caption = request.Caption,
            Hashtags = request.Hashtags.ToArray(),
            Cta = request.Cta,
            MediaUrls = request.MediaUrls.ToArray(),
            MediaFormat = request.MediaFormat ?? "1:1",
            Status = status,
            ScheduledAt = request.ScheduledAt,
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow
        };

        await _contentRepository.CreateAsync(item, ct);

        if (item.Type == "Reel" && request.Scenes.Any())
        {
            var scenes = request.Scenes.Select(s => new ReelScene
            {
                SceneOrder = s.SceneOrder,
                ScriptText = s.ScriptText,
                VisualDescription = s.VisualDescription,
                DurationSeconds = s.DurationSeconds > 0 ? s.DurationSeconds : 5
            });

            await _contentRepository.SaveReelScenesAsync(item.Id, _tenantContext.TenantId, scenes, ct);
        }

        return Result.Ok(item.Id);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _contentRepository.DeleteAsync(id, _tenantContext.TenantId, ct);
        return Result.Ok();
    }

    private static ContentItemDto Map(ContentItem item) => new()
    {
        Id = item.Id,
        InstagramAccountId = item.InstagramAccountId,
        Type = item.Type,
        Title = item.Title,
        Caption = item.Caption,
        Hashtags = item.Hashtags?.ToList() ?? new List<string>(),
        Cta = item.Cta,
        MediaUrls = item.MediaUrls?.ToList() ?? new List<string>(),
        MediaFormat = item.MediaFormat,
        Status = item.Status,
        ScheduledAt = item.ScheduledAt,
        PublishedAt = item.PublishedAt,
        ErrorMessage = item.ErrorMessage,
        CreatedAt = item.CreatedAt,
        Scenes = item.Scenes.Select(s => new ReelSceneDto
        {
            SceneOrder = s.SceneOrder,
            ScriptText = s.ScriptText,
            VisualDescription = s.VisualDescription,
            DurationSeconds = s.DurationSeconds
        }).ToList()
    };
}
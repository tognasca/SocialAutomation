using SocialAutomation.Application.AI.Dtos;
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.AI.Services;

public class KnowledgeService
{
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public KnowledgeService(
        IKnowledgeRepository knowledgeRepository,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _knowledgeRepository = knowledgeRepository;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IEnumerable<KnowledgeDocumentDto>>> GetDocumentsAsync(CancellationToken ct = default)
    {
        var docs = await _knowledgeRepository.GetDocumentsAsync(_tenantContext.TenantId, null, ct);
        return Result.Ok(docs.Select(d => new KnowledgeDocumentDto
        {
            Id = d.Id,
            KnowledgeBaseId = d.KnowledgeBaseId,
            Title = d.Title,
            Content = d.Content,
            SourceType = d.SourceType
        }));
    }

    public async Task<Result<Guid>> CreateDocumentAsync(CreateKnowledgeDocumentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return Result.Fail<Guid>("Título e conteúdo são obrigatórios.");

        Guid kbId;
        if (request.KnowledgeBaseId.HasValue)
        {
            kbId = request.KnowledgeBaseId.Value;
        }
        else
        {
            var bases = (await _knowledgeRepository.GetBasesAsync(_tenantContext.TenantId, ct)).ToList();
            if (bases.Count == 0)
            {
                kbId = Guid.NewGuid();
                await _knowledgeRepository.CreateBaseAsync(new KnowledgeBase
                {
                    Id = kbId,
                    TenantId = _tenantContext.TenantId,
                    Name = "Base Principal",
                    Description = "Base padrão do tenant",
                    IsActive = true,
                    CreatedAt = _dateTimeProvider.UtcNow,
                    UpdatedAt = _dateTimeProvider.UtcNow
                }, ct);
            }
            else
            {
                kbId = bases[0].Id;
            }
        }

        var doc = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            KnowledgeBaseId = kbId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            SourceType = request.SourceType,
            IsActive = true,
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow
        };

        await _knowledgeRepository.CreateDocumentAsync(doc, ct);

        // Chunk simples (~800 chars)
        var chunks = SplitIntoChunks(doc.Content, 800)
            .Select((text, index) => new KnowledgeChunk
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                DocumentId = doc.Id,
                ChunkIndex = index,
                Content = text,
                TokenCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                CreatedAt = _dateTimeProvider.UtcNow
            }).ToList();

        await _knowledgeRepository.CreateChunksAsync(chunks, ct);
        return Result.Ok(doc.Id);
    }

    private static IEnumerable<string> SplitIntoChunks(string content, int size)
    {
        for (var i = 0; i < content.Length; i += size)
            yield return content.Substring(i, Math.Min(size, content.Length - i));
    }
}
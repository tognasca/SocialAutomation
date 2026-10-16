using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.Content.DTOs;
using SocialAutomation.Application.Content.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/content")]
[Authorize]
public class ContentController : ControllerBase
{
    private readonly ContentService _contentService;
    private readonly ContentGeneratorService _generatorService;

    public ContentController(ContentService contentService, ContentGeneratorService generatorService)
    {
        _contentService = contentService;
        _generatorService = generatorService;
    }

    [HttpGet]
    [RequirePermission("content.read")]
    public async Task<IActionResult> GetAll([FromQuery] string? type, [FromQuery] string? status, CancellationToken ct)
    {
        var result = await _contentService.GetAllAsync(type, status, ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("content.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _contentService.GetByIdAsync(id, ct);
        if (!result.Success) return NotFound(new { success = false, error = result.Error });

        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost]
    [RequirePermission("content.create")]
    public async Task<IActionResult> Create([FromBody] CreateContentItemRequest request, CancellationToken ct)
    {
        var result = await _contentService.CreateAsync(request, ct);
        if (!result.Success) return BadRequest(new { success = false, error = result.Error });

        return Created($"/api/content/{result.Data}", new { success = true, data = new { id = result.Data } });
    }

    [HttpPost("generate-ai")]
    [RequirePermission("content.create")]
    public async Task<IActionResult> GenerateWithAi([FromBody] GenerateAiContentRequest request, CancellationToken ct)
    {
        var result = await _generatorService.GenerateAsync(request, ct);
        if (!result.Success) return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true, data = result.Data });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("content.create")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _contentService.DeleteAsync(id, ct);
        return Ok(new { success = true });
    }
}
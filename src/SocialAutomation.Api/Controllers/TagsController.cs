
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Application.CRM.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly TagService _tagService;

    public TagsController(TagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _tagService.GetAllAsync(ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTagDto request, CancellationToken ct)
    {
        var result = await _tagService.CreateAsync(request.Name, request.Color, ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("{tagId:guid}/leads/{leadId:guid}")]
    public async Task<IActionResult> AssignToLead(Guid tagId, Guid leadId, CancellationToken ct)
    {
        await _tagService.AssignToLeadAsync(leadId, tagId, ct);
        return Ok(new { success = true });
    }

    [HttpDelete("{tagId:guid}/leads/{leadId:guid}")]
    public async Task<IActionResult> RemoveFromLead(Guid tagId, Guid leadId, CancellationToken ct)
    {
        await _tagService.RemoveFromLeadAsync(leadId, tagId, ct);
        return Ok(new { success = true });
    }

    public record CreateTagDto(string Name, string Color);
}
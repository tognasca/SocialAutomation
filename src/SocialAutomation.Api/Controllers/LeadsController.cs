
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.Common;
using SocialAutomation.Application.CRM.DTOs;
using SocialAutomation.Application.CRM.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/leads")]
[Authorize]
public class LeadsController : ControllerBase
{
    private readonly LeadService _leadService;

    public LeadsController(LeadService leadService)
    {
        _leadService = leadService;
    }

    [HttpGet]
    [RequirePermission("leads.read")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] PagedRequest request,
        [FromQuery] string? status,
        [FromQuery] string? stage,
        [FromQuery] Guid? tagId,
        [FromQuery] Guid? ownerUserId,
        CancellationToken ct)
    {
        var result = await _leadService.GetPagedAsync(request, status, stage, tagId, ownerUserId, ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("leads.read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _leadService.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(new { success = false, error = result.Error });

        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost]
    [RequirePermission("leads.create")]
    public async Task<IActionResult> Create([FromBody] CreateLeadRequest request, CancellationToken ct)
    {
        var result = await _leadService.CreateAsync(request, ct);
        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Created($"/api/leads/{result.Data}", new { success = true, data = new { id = result.Data } });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("leads.update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadRequest request, CancellationToken ct)
    {
        var result = await _leadService.UpdateAsync(id, request, ct);
        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true });
    }

    [HttpPut("{id:guid}/stage")]
    [RequirePermission("leads.update")]
    public async Task<IActionResult> MoveStage(Guid id, [FromBody] MoveLeadStageRequest request, CancellationToken ct)
    {
        var result = await _leadService.MoveStageAsync(id, request, ct);
        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true });
    }

    [HttpGet("{id:guid}/activities")]
    [RequirePermission("leads.read")]
    public async Task<IActionResult> GetActivities(Guid id, CancellationToken ct)
    {
        var result = await _leadService.GetActivitiesAsync(id, ct);
        return Ok(new { success = true, data = result.Data });
    }
}
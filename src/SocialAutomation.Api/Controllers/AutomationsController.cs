
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.Automation.DTOs;
using SocialAutomation.Application.Automation.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/automations")]
[Authorize]
public class AutomationsController : ControllerBase
{
    private readonly AutomationService _automationService;

    public AutomationsController(AutomationService automationService)
    {
        _automationService = automationService;
    }

    [HttpGet]
    [RequirePermission("automations.read")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _automationService.GetAllAsync(ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost]
    [RequirePermission("automations.create")]
    public async Task<IActionResult> Create([FromBody] CreateAutomationFlowRequest request, CancellationToken ct)
    {
        var result = await _automationService.CreateFlowAsync(request, ct);
        if (!result.Success) return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true, data = new { id = result.Data } });
    }

    [HttpPut("{id:guid}/toggle")]
    [RequirePermission("automations.create")]
    public async Task<IActionResult> Toggle(Guid id, [FromBody] ToggleAutomationRequest request, CancellationToken ct)
    {
        await _automationService.ToggleActiveAsync(id, request.IsActive, ct);
        return Ok(new { success = true });
    }

    public record ToggleAutomationRequest(bool IsActive);
}
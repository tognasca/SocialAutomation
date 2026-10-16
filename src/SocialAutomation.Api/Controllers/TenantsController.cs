// src/SocialAutomation.Api/Controllers/TenantsController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.Tenants.DTOs;
using SocialAutomation.Application.Tenants.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly TenantService _tenantService;

    public TenantsController(TenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var result = await _tenantService.GetCurrentAsync(ct);

        if (!result.Success)
            return NotFound(new { success = false, error = result.Error });

        return Ok(new { success = true, data = result.Data });
    }

    [HttpPut("current")]
    [RequirePermission("settings.manage")]
    public async Task<IActionResult> UpdateCurrent([FromBody] UpdateTenantRequest request, CancellationToken ct)
    {
        var result = await _tenantService.UpdateCurrentAsync(request, ct);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.Automation.DTOs;
using SocialAutomation.Application.Automation.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/campaigns")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly CampaignService _campaignService;

    public CampaignsController(CampaignService campaignService)
    {
        _campaignService = campaignService;
    }

    [HttpGet]
    [RequirePermission("campaigns.read")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _campaignService.GetAllAsync(ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost]
    [RequirePermission("campaigns.create")]
    public async Task<IActionResult> Create([FromBody] CreateCampaignRequest request, CancellationToken ct)
    {
        var result = await _campaignService.CreateCampaignAsync(request, ct);
        if (!result.Success) return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true, data = new { id = result.Data } });
    }
}
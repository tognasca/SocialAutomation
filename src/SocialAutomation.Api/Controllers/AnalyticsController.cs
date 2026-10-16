
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.Analytics.DTOs;
using SocialAutomation.Application.Analytics.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _analyticsService;

    public AnalyticsController(AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("dashboard")]
    [RequirePermission("analytics.read")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string period = "30d",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await _analyticsService.GetDashboardAsync(new AnalyticsFilterRequest
        {
            Period = period,
            From = from,
            To = to
        }, ct);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true, data = result.Data });
    }
}
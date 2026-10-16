
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.CRM.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/pipeline")]
[Authorize]
public class PipelineController : ControllerBase
{
    private readonly PipelineService _pipelineService;

    public PipelineController(PipelineService pipelineService)
    {
        _pipelineService = pipelineService;
    }

    [HttpGet("board")]
    [RequirePermission("leads.read")]
    public async Task<IActionResult> GetBoard(CancellationToken ct)
    {
        var result = await _pipelineService.GetBoardAsync(ct);
        return Ok(new { success = true, data = result.Data });
    }
}
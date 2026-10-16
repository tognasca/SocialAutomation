
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Application.Common;
using SocialAutomation.Application.CRM.DTOs;
using SocialAutomation.Application.CRM.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly CrmTaskService _taskService;

    public TasksController(CrmTaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] PagedRequest request, [FromQuery] string? status, [FromQuery] Guid? assignedUserId, CancellationToken ct)
    {
        var result = await _taskService.GetPagedAsync(request, status, assignedUserId, ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var result = await _taskService.CreateAsync(request, ct);
        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Created($"/api/tasks/{result.Data}", new { success = true, data = new { id = result.Data } });
    }

    [HttpPut("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _taskService.CompleteAsync(id, ct);
        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true });
    }
}
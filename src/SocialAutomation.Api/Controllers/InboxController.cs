using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Inbox.DTOs;
using SocialAutomation.Application.Inbox.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/inbox")]
[Authorize]
public class InboxController : ControllerBase
{
    private readonly InboxService _inboxService;

    public InboxController(InboxService inboxService)
    {
        _inboxService = inboxService;
    }

    [HttpGet("conversations")]
    [RequirePermission("conversations.read")]
    public async Task<IActionResult> GetConversations(
        [FromQuery] PagedRequest request, [FromQuery] string? status, [FromQuery] Guid? assignedUserId, CancellationToken ct)
    {
        var result = await _inboxService.GetConversationsAsync(request, status, assignedUserId, ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet("conversations/{id:guid}/messages")]
    [RequirePermission("conversations.read")]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken ct)
    {
        var result = await _inboxService.GetMessagesAsync(id, ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("conversations/{id:guid}/messages")]
    [RequirePermission("conversations.reply")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendInboxMessageRequest request, CancellationToken ct)
    {
        var result = await _inboxService.SendMessageAsync(id, request, ct);
        if (!result.Success) return BadRequest(new { success = false, error = result.Error });
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPut("conversations/{id:guid}/assign")]
    [RequirePermission("conversations.reply")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignUserRequest request, CancellationToken ct)
    {
        var result = await _inboxService.AssignUserAsync(id, request.UserId, ct);
        return Ok(new { success = true });
    }

    [HttpPut("conversations/{id:guid}/mode")]
    [RequirePermission("conversations.reply")]
    public async Task<IActionResult> ChangeMode(Guid id, [FromBody] ChangeModeRequest request, CancellationToken ct)
    {
        var result = await _inboxService.ChangeModeAsync(id, request.Mode, ct);
        return Ok(new { success = true });
    }
}

public record AssignUserRequest(Guid? UserId);
public record ChangeModeRequest(string Mode);
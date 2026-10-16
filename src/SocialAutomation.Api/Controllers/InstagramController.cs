using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.Instagram.DTOs;
using SocialAutomation.Application.Instagram.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/instagram")]
[Authorize]
public class InstagramController : ControllerBase
{
    private readonly InstagramAccountService _accountService;
    private readonly InstagramMessageService _messageService;

    public InstagramController(
        InstagramAccountService accountService,
        InstagramMessageService messageService)
    {
        _accountService = accountService;
        _messageService = messageService;
    }

    [HttpGet("oauth-url")]
    [RequirePermission("instagram.connect")]
    public async Task<IActionResult> GetOAuthUrl([FromQuery] string redirectUri, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
            return BadRequest(new { success = false, error = "redirectUri é obrigatória." });

        var result = await _accountService.GenerateOAuthUrlAsync(redirectUri, ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("connect")]
    [RequirePermission("instagram.connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectInstagramCallbackRequest request, CancellationToken ct)
    {
        var result = await _accountService.HandleOAuthCallbackAsync(request, ct);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet("accounts")]
    [RequirePermission("instagram.manage")]
    public async Task<IActionResult> GetAccounts(CancellationToken ct)
    {
        var result = await _accountService.GetAccountsAsync(ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpDelete("accounts/{id:guid}")]
    [RequirePermission("instagram.manage")]
    public async Task<IActionResult> Disconnect(Guid id, CancellationToken ct)
    {
        var result = await _accountService.DisconnectAsync(id, ct);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true });
    }

    [HttpPost("messages/send")]
    [RequirePermission("conversations.reply")]
    public async Task<IActionResult> SendMessage([FromBody] SendInstagramMessageRequest request, CancellationToken ct)
    {
        var result = await _messageService.SendDirectMessageAsync(request, ct);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true, data = result.Data });
    }
}
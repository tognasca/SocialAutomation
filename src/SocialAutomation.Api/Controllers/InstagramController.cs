using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;
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
    private readonly IConfiguration _configuration;

    public InstagramController(
        InstagramAccountService accountService,
        InstagramMessageService messageService, IConfiguration configuration)
    {
        _accountService = accountService;
        _messageService = messageService;
        _configuration = configuration;
    }

    [HttpGet("oauth-url")]
    [RequirePermission("instagram.connect")]
    public async Task<IActionResult> GetOAuthUrl([FromQuery] string redirectUri, CancellationToken ct)
    {
        if (!IsAllowedCallback(redirectUri))
            return BadRequest(new { success = false, error = "Endereço de retorno do Instagram inválido." });

        if (!HasInstagramConfiguration())
            return StatusCode(503, new { success = false, error = "A integração com a Meta ainda não foi configurada." });

        var result = await _accountService.GenerateOAuthUrlAsync(redirectUri, ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("connect")]
    [RequirePermission("instagram.connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectInstagramCallbackRequest request, CancellationToken ct)
    {
        if (!IsAllowedCallback(request.RedirectUri))
            return BadRequest(new { success = false, error = "Endereço de retorno do Instagram inválido." });

        if (!HasInstagramConfiguration())
            return StatusCode(503, new { success = false, error = "A integração com a Meta ainda não foi configurada." });

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

    [HttpGet("publication-accounts")]
    [RequirePermission("content.publish")]
    public Task<IActionResult> GetPublicationAccounts(CancellationToken ct) => GetAccounts(ct);

    [HttpGet("search")]
    [RequirePermission("instagram.manage")]
    public async Task<IActionResult> Search(
        [FromQuery] Guid accountId, [FromQuery] string query, [FromQuery] bool hashtag,
        [FromServices] IInstagramAccountRepository repository,
        [FromServices] IInstagramClient client,
        [FromServices] ITenantContext tenant,
        CancellationToken ct)
    {
        var normalized = query.Trim().TrimStart(hashtag ? '#' : '@');
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized,
                hashtag ? @"^[\p{L}\p{M}\p{Nd}_]{1,100}$" : @"^[a-zA-Z0-9_.]{1,30}$"))
            return BadRequest(new { success = false, error = "Informe uma hashtag ou um usuário válido." });

        var account = await repository.GetByIdAsync(accountId, tenant.TenantId, ct);
        if (account?.Status != InstagramAccountStatus.Active)
            return NotFound(new { success = false, error = "Conta ativa do Instagram não encontrada." });

        if (string.IsNullOrEmpty(account.UserAccessToken) || account.TokenExpiresAt <= DateTime.UtcNow)
            return BadRequest(new { success = false, error = "Reconecte a conta do Instagram para pesquisar." });

        try
        {
            var result = await client.SearchAsync(account.InstagramBusinessId, account.UserAccessToken, normalized, hashtag, ct);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { success = false, error = ex.Message });
        }
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
    private bool HasInstagramConfiguration() =>
        !string.IsNullOrWhiteSpace(_configuration["Instagram:AppId"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Instagram:AppSecret"]) &&
        _configuration["Instagram:TokenEncryptionKey"] is { Length: >= 32 };

    private bool IsAllowedCallback(string callback)
    {
        if (!Uri.TryCreate(callback, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)) ||
            uri.AbsolutePath != "/instagram/callback" || uri.Query.Length != 0 ||
            uri.Fragment.Length != 0 || uri.UserInfo.Length != 0)
            return false;

        var origins = (_configuration["App:AllowedOrigins"] ?? "http://localhost:3100;http://localhost:3101")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return origins.Contains(uri.GetLeftPart(UriPartial.Authority), StringComparer.OrdinalIgnoreCase);
    }
}

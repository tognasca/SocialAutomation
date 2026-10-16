// src/SocialAutomation.Api/Controllers/AuthController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Application.Auth.DTOs;
using SocialAutomation.Application.Auth.Services;
using SocialAutomation.Application.Common.Interfaces;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ITenantContext _tenantContext;

    public AuthController(AuthService authService, ITenantContext tenantContext)
    {
        _authService = authService;
        _tenantContext = tenantContext;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);

        if (!result.Success)
            return Unauthorized(new { success = false, error = result.Error });

        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshAsync(request, ct);

        if (!result.Success)
            return Unauthorized(new { success = false, error = result.Error });

        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var result = await _authService.LogoutAsync(_tenantContext.UserId, ct);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? User.FindFirst("email")?.Value;
        var name = User.FindFirst("name")?.Value;
        var tenantId = User.FindFirst("tenant_id")?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = User.FindAll("permission").Select(c => c.Value).ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                id = userId,
                email,
                name,
                tenantId,
                roles,
                permissions
            }
        });
    }
}
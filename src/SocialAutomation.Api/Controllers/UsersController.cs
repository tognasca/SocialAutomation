// src/SocialAutomation.Api/Controllers/UsersController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Users.DTOs;
using SocialAutomation.Application.Users.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [RequirePermission("users.manage")]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _userService.GetPagedAsync(request, ct);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("users.manage")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _userService.GetByIdAsync(id, ct);

        if (!result.Success)
            return NotFound(new { success = false, error = result.Error });

        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost]
    [RequirePermission("users.manage")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _userService.CreateAsync(request, ct);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Created($"/api/users/{result.Data}", new { success = true, data = new { id = result.Data } });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("users.manage")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _userService.UpdateAsync(id, request, ct);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("users.manage")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await _userService.DeactivateAsync(id, ct);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true });
    }

    [HttpPut("{id:guid}/roles")]
    [RequirePermission("users.manage")]
    public async Task<IActionResult> UpdateRoles(Guid id, [FromBody] UpdateUserRolesRequest request, CancellationToken ct)
    {
        var result = await _userService.UpdateRolesAsync(id, request, ct);

        if (!result.Success)
            return BadRequest(new { success = false, error = result.Error });

        return Ok(new { success = true });
    }
}
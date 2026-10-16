// src/SocialAutomation.Api/Controllers/PermissionsController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionRepository _permissionRepository;

    public PermissionsController(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var permissions = await _permissionRepository.GetAllAsync(ct);

        var grouped = permissions.GroupBy(p => p.Module).Select(g => new
        {
            module = g.Key,
            permissions = g.Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.Description
            })
        });

        return Ok(new { success = true, data = grouped });
    }
}
// src/SocialAutomation.Api/Controllers/RolesController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantContext _tenantContext;

    public RolesController(IRoleRepository roleRepository, ITenantContext tenantContext)
    {
        _roleRepository = roleRepository;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var roles = await _roleRepository.GetAllAsync(_tenantContext.TenantId, ct);

        return Ok(new
        {
            success = true,
            data = roles.Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.IsSystem
            })
        });
    }
}
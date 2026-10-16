// src/SocialAutomation.Api/Filters/PermissionAuthorizationFilter.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SocialAutomation.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissions = user.FindAll("permission").Select(c => c.Value);
        var roles = user.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value);

        if (roles.Contains("SuperAdmin"))
            return;

        if (!permissions.Contains(_permission))
        {
            context.Result = new ForbidResult();
        }
    }
}
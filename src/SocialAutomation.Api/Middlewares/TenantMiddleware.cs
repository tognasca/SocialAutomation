// src/SocialAutomation.Api/Middlewares/TenantMiddleware.cs

using SocialAutomation.Application.Common.Interfaces;

namespace SocialAutomation.Api.Middlewares;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
            var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? context.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(tenantIdClaim, out var tenantId) &&
                Guid.TryParse(userIdClaim, out var userId))
            {
                var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
                tenantContext.Set(tenantId, userId);
            }
        }

        await _next(context);
    }
}
// src/SocialAutomation.Application/Tenants/DTOs/UpdateTenantRequest.cs

namespace SocialAutomation.Application.Tenants.DTOs;

public class UpdateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Document { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
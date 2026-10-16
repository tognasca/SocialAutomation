// src/SocialAutomation.Application/Tenants/DTOs/TenantResponse.cs

namespace SocialAutomation.Application.Tenants.DTOs;

public class TenantResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Document { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Plan { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int MaxInstagramAccounts { get; set; }
    public DateTime CreatedAt { get; set; }
}
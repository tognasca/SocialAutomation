using SocialAutomation.Domain.Enums;
namespace SocialAutomation.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Document { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Plan { get; set; } = "free";
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public string? Settings { get; set; }
    public int MaxUsers { get; set; } = 5;
    public int MaxInstagramAccounts { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
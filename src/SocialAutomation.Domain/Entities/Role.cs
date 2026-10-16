// src/SocialAutomation.Domain/Entities/Role.cs

namespace SocialAutomation.Domain.Entities;

public class Role
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
}
// src/SocialAutomation.Domain/Entities/User.cs

using SocialAutomation.Domain.Enums;

namespace SocialAutomation.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool EmailConfirmed { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation (populated by service, not by ORM)
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}
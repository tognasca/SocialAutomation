// src/SocialAutomation.Domain/Entities/RolePermission.cs

namespace SocialAutomation.Domain.Entities;

public class RolePermission
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
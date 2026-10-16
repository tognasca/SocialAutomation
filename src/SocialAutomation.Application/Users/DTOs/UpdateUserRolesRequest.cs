
namespace SocialAutomation.Application.Users.DTOs;

public class UpdateUserRolesRequest
{
    public List<Guid> RoleIds { get; set; } = new();
}
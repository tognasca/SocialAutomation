// src/SocialAutomation.Application/Users/DTOs/UpdateUserRequest.cs

namespace SocialAutomation.Application.Users.DTOs;

public class UpdateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Roles { get; set; } = new();
}
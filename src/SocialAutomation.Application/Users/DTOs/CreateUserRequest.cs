// src/SocialAutomation.Application/Users/DTOs/CreateUserRequest.cs

namespace SocialAutomation.Application.Users.DTOs;

public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public List<string> Roles { get; set; } = new();
}
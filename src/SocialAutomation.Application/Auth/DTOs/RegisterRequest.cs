// src/SocialAutomation.Application/Auth/DTOs/RegisterRequest.cs

namespace SocialAutomation.Application.Auth.DTOs;

public class RegisterRequest
{
    public string TenantName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
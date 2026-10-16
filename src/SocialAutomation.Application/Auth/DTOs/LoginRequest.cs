// src/SocialAutomation.Application/Auth/DTOs/LoginRequest.cs

namespace SocialAutomation.Application.Auth.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
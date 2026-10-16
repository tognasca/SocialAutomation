// src/SocialAutomation.Application/Auth/DTOs/RefreshTokenRequest.cs

namespace SocialAutomation.Application.Auth.DTOs;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
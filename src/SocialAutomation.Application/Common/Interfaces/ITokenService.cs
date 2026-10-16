// src/SocialAutomation.Application/Auth/Services/ITokenService.cs

using SocialAutomation.Domain.Entities;

namespace  SocialAutomation.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
}
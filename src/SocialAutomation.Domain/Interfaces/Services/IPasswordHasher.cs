// src/SocialAutomation.Domain/Interfaces/Services/IPasswordHasher.cs

namespace SocialAutomation.Domain.Interfaces.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
// src/SocialAutomation.Domain/Interfaces/Services/IDateTimeProvider.cs

namespace SocialAutomation.Domain.Interfaces.Services;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
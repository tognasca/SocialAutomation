// src/SocialAutomation.Infrastructure/Services/DateTimeProvider.cs

using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
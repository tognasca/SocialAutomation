using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IIntegrationLogRepository
{
    Task CreateAsync(IntegrationLog log, CancellationToken ct = default);
}
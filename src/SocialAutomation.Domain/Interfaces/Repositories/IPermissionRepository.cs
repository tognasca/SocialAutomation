// src/SocialAutomation.Domain/Interfaces/Repositories/IPermissionRepository.cs

using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IPermissionRepository
{
    Task<IEnumerable<Permission>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<string>> GetUserPermissionCodesAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Permission>> GetByModuleAsync(string module, CancellationToken ct = default);
}
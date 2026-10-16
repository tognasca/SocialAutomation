// src/SocialAutomation.Domain/Interfaces/Repositories/IRoleRepository.cs

using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<IEnumerable<Role>> GetAllAsync(Guid? tenantId = null, CancellationToken ct = default);
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Role?> GetAssignableByNameAsync(string name, Guid tenantId, CancellationToken ct = default);
    Task<Role?> GetAssignableByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<string>> GetUserRoleNamesAsync(Guid userId, CancellationToken ct = default);
    Task AssignRoleToUserAsync(Guid userId, Guid roleId, CancellationToken ct = default);
    Task RemoveAllUserRolesAsync(Guid userId, CancellationToken ct = default);
}
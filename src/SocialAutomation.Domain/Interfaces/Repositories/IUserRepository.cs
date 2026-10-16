using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, Guid tenantId, CancellationToken ct = default);
    Task<User?> GetByEmailGlobalAsync(string email, CancellationToken ct = default);
    Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<Guid> CreateAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task UpdateLastLoginAsync(Guid id, DateTime lastLoginAt, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, Guid tenantId, CancellationToken ct = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
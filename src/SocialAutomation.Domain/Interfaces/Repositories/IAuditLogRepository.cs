// src/SocialAutomation.Domain/Interfaces/Repositories/IAuditLogRepository.cs

using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task CreateAsync(AuditLog auditLog, CancellationToken ct = default);
    Task<(IEnumerable<AuditLog> Items, int Total)> GetPagedAsync(Guid tenantId, int page, int pageSize, string? action = null, CancellationToken ct = default);
}
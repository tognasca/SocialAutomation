using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public PermissionRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Permission>> GetAllAsync(CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Permission>(
            @"SELECT id, code, name, description, module, created_at AS CreatedAt
              FROM permissions ORDER BY module, code");
    }

    public async Task<IEnumerable<string>> GetUserPermissionCodesAsync(Guid userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<string>(
            @"SELECT DISTINCT p.code
              FROM permissions p
              INNER JOIN role_permissions rp ON rp.permission_id = p.id
              INNER JOIN user_roles ur ON ur.role_id = rp.role_id
              WHERE ur.user_id = @UserId",
            new { UserId = userId });
    }

    public async Task<IEnumerable<Permission>> GetByModuleAsync(string module, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Permission>(
            @"SELECT id, code, name, description, module, created_at AS CreatedAt
              FROM permissions WHERE module = @Module ORDER BY code",
            new { Module = module });
    }
}
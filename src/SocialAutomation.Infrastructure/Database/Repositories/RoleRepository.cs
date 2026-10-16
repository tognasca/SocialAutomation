// src/SocialAutomation.Infrastructure/Database/Repositories/RoleRepository.cs

using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public RoleRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Role>> GetAllAsync(Guid? tenantId = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Role>(
            @"SELECT id, tenant_id AS TenantId, name, description, is_system AS IsSystem,
                     created_at AS CreatedAt
              FROM roles
              WHERE tenant_id IS NULL OR tenant_id = @TenantId
              ORDER BY name",
            new { TenantId = tenantId });
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Role>(
            @"SELECT id, tenant_id AS TenantId, name, description, is_system AS IsSystem,
                     created_at AS CreatedAt
              FROM roles WHERE id = @Id",
            new { Id = id });
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Role>(
            @"SELECT id, tenant_id AS TenantId, name, description, is_system AS IsSystem,
                     created_at AS CreatedAt
              FROM roles WHERE name = @Name",
            new { Name = name });
    }

    public async Task<IEnumerable<string>> GetUserRoleNamesAsync(Guid userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<string>(
            @"SELECT r.name
              FROM roles r
              INNER JOIN user_roles ur ON ur.role_id = r.id
              WHERE ur.user_id = @UserId",
            new { UserId = userId });
    }

    public async Task AssignRoleToUserAsync(Guid userId, Guid roleId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO user_roles (id, user_id, role_id, assigned_at)
              VALUES (@Id, @UserId, @RoleId, @AssignedAt)
              ON CONFLICT (user_id, role_id) DO NOTHING",
            new
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            });
    }

    public async Task RemoveAllUserRolesAsync(Guid userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM user_roles WHERE user_id = @UserId",
            new { UserId = userId });
    }
}
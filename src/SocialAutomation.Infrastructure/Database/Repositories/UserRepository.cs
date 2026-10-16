// src/SocialAutomation.Infrastructure/Database/Repositories/UserRepository.cs

using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public UserRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        if (tenantId == Guid.Empty)
        {
            return await connection.QueryFirstOrDefaultAsync<User>(
                @"SELECT id, tenant_id AS TenantId, name, email, password_hash AS PasswordHash,
                         phone, avatar_url AS AvatarUrl, status, email_confirmed AS EmailConfirmed,
                         last_login_at AS LastLoginAt, created_at AS CreatedAt, updated_at AS UpdatedAt
                  FROM users WHERE id = @Id",
                new { Id = id });
        }

        return await connection.QueryFirstOrDefaultAsync<User>(
            @"SELECT id, tenant_id AS TenantId, name, email, password_hash AS PasswordHash,
                     phone, avatar_url AS AvatarUrl, status, email_confirmed AS EmailConfirmed,
                     last_login_at AS LastLoginAt, created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM users WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = tenantId });
    }

    public async Task<User?> GetByEmailAsync(string email, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            @"SELECT id, tenant_id AS TenantId, name, email, password_hash AS PasswordHash,
                     phone, avatar_url AS AvatarUrl, status, email_confirmed AS EmailConfirmed,
                     last_login_at AS LastLoginAt, created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM users WHERE email = @Email AND tenant_id = @TenantId",
            new { Email = email, TenantId = tenantId });
    }

    public async Task<User?> GetByEmailGlobalAsync(string email, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            @"SELECT id, tenant_id AS TenantId, name, email, password_hash AS PasswordHash,
                     phone, avatar_url AS AvatarUrl, status, email_confirmed AS EmailConfirmed,
                     last_login_at AS LastLoginAt, created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM users WHERE email = @Email ORDER BY created_at LIMIT 1",
            new { Email = email });
    }

    public async Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(
        Guid tenantId, int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var whereClause = "WHERE tenant_id = @TenantId";
        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause += " AND (name ILIKE @Search OR email ILIKE @Search)";
        }

        var total = await connection.QueryFirstOrDefaultAsync<int>(
            $"SELECT COUNT(*) FROM users {whereClause}",
            new { TenantId = tenantId, Search = $"%{search}%" });

        var items = await connection.QueryAsync<User>(
            $@"SELECT id, tenant_id AS TenantId, name, email, password_hash AS PasswordHash,
                      phone, avatar_url AS AvatarUrl, status, email_confirmed AS EmailConfirmed,
                      last_login_at AS LastLoginAt, created_at AS CreatedAt, updated_at AS UpdatedAt
               FROM users {whereClause}
               ORDER BY created_at DESC
               LIMIT @PageSize OFFSET @Offset",
            new
            {
                TenantId = tenantId,
                Search = $"%{search}%",
                PageSize = pageSize,
                Offset = (page - 1) * pageSize
            });

        return (items, total);
    }

    public async Task<Guid> CreateAsync(User user, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO users (id, tenant_id, name, email, password_hash, phone, avatar_url,
                                  status, email_confirmed, created_at, updated_at)
              VALUES (@Id, @TenantId, @Name, @Email, @PasswordHash, @Phone, @AvatarUrl,
                      @Status, @EmailConfirmed, @CreatedAt, @UpdatedAt)",
            new
            {
                user.Id,
                user.TenantId,
                user.Name,
                user.Email,
                user.PasswordHash,
                user.Phone,
                user.AvatarUrl,
                Status = (short)user.Status,
                user.EmailConfirmed,
                user.CreatedAt,
                user.UpdatedAt
            });

        return user.Id;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE users
              SET name = @Name, phone = @Phone, avatar_url = @AvatarUrl,
                  status = @Status, updated_at = @UpdatedAt
              WHERE id = @Id AND tenant_id = @TenantId",
            new
            {
                user.Id,
                user.TenantId,
                user.Name,
                user.Phone,
                user.AvatarUrl,
                Status = (short)user.Status,
                user.UpdatedAt
            });
    }

    public async Task UpdateLastLoginAsync(Guid id, DateTime lastLoginAt, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE users SET last_login_at = @LastLoginAt WHERE id = @Id",
            new { Id = id, LastLoginAt = lastLoginAt });
    }

    public async Task<bool> EmailExistsAsync(string email, Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM users WHERE email = @Email AND tenant_id = @TenantId)",
            new { Email = email, TenantId = tenantId });
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM users WHERE tenant_id = @TenantId",
            new { TenantId = tenantId });
    }
}
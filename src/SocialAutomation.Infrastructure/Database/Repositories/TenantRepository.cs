// src/SocialAutomation.Infrastructure/Database/Repositories/TenantRepository.cs

using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public TenantRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Tenant>(
            @"SELECT id, name, slug, document, email, phone, plan,
                     status, settings, max_users AS MaxUsers,
                     max_instagram_accounts AS MaxInstagramAccounts,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM tenants WHERE id = @Id",
            new { Id = id });
    }

    public async Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Tenant>(
            @"SELECT id, name, slug, document, email, phone, plan,
                     status, settings, max_users AS MaxUsers,
                     max_instagram_accounts AS MaxInstagramAccounts,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM tenants WHERE slug = @Slug",
            new { Slug = slug });
    }

    public async Task<Guid> CreateAsync(Tenant tenant, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO tenants (id, name, slug, document, email, phone, plan, status, settings,
                                    max_users, max_instagram_accounts, created_at, updated_at)
              VALUES (@Id, @Name, @Slug, @Document, @Email, @Phone, @Plan, @Status, @Settings::jsonb,
                      @MaxUsers, @MaxInstagramAccounts, @CreatedAt, @UpdatedAt)",
            new
            {
                tenant.Id,
                tenant.Name,
                tenant.Slug,
                tenant.Document,
                tenant.Email,
                tenant.Phone,
                tenant.Plan,
                Status = (short)tenant.Status,
                Settings = tenant.Settings ?? "{}",
                tenant.MaxUsers,
                tenant.MaxInstagramAccounts,
                tenant.CreatedAt,
                tenant.UpdatedAt
            });

        return tenant.Id;
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE tenants
              SET name = @Name, document = @Document, email = @Email,
                  phone = @Phone, updated_at = @UpdatedAt
              WHERE id = @Id",
            new
            {
                tenant.Id,
                tenant.Name,
                tenant.Document,
                tenant.Email,
                tenant.Phone,
                tenant.UpdatedAt
            });
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM tenants WHERE slug = @Slug)",
            new { Slug = slug });
    }
}
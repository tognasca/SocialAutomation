// src/SocialAutomation.Infrastructure/Database/Repositories/RefreshTokenRepository.cs

using Dapper;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Infrastructure.Database.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<RefreshToken>(
            @"SELECT id, user_id AS UserId, token, expires_at AS ExpiresAt,
                     created_at AS CreatedAt, revoked_at AS RevokedAt,
                     replaced_by AS ReplacedBy, is_revoked AS IsRevoked
              FROM refresh_tokens WHERE token = @Token",
            new { Token = token });
    }

    public async Task CreateAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO refresh_tokens (id, user_id, token, expires_at, created_at, is_revoked)
              VALUES (@Id, @UserId, @Token, @ExpiresAt, @CreatedAt, @IsRevoked)",
            new
            {
                refreshToken.Id,
                refreshToken.UserId,
                refreshToken.Token,
                refreshToken.ExpiresAt,
                refreshToken.CreatedAt,
                refreshToken.IsRevoked
            });
    }

    public async Task RevokeAsync(Guid id, string? replacedBy = null, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE refresh_tokens
              SET is_revoked = TRUE, revoked_at = @RevokedAt, replaced_by = @ReplacedBy
              WHERE id = @Id",
            new { Id = id, RevokedAt = DateTime.UtcNow, ReplacedBy = replacedBy });
    }

    public async Task RevokeAllByUserAsync(Guid userId, CancellationToken ct = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE refresh_tokens
              SET is_revoked = TRUE, revoked_at = @RevokedAt
              WHERE user_id = @UserId AND is_revoked = FALSE",
            new { UserId = userId, RevokedAt = DateTime.UtcNow });
    }
}
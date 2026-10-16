// src/SocialAutomation.Domain/Interfaces/Repositories/IRefreshTokenRepository.cs

using SocialAutomation.Domain.Entities;

namespace SocialAutomation.Domain.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task CreateAsync(RefreshToken refreshToken, CancellationToken ct = default);
    Task RevokeAsync(Guid id, string? replacedBy = null, CancellationToken ct = default);
    Task RevokeAllByUserAsync(Guid userId, CancellationToken ct = default);
}
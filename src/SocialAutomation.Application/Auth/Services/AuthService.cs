// src/SocialAutomation.Application/Auth/Services/AuthService.cs

using SocialAutomation.Application.Auth.DTOs;
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;
using System.Text.RegularExpressions;

namespace SocialAutomation.Application.Auth.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly int _refreshTokenDays;

    public AuthService(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider,
        int refreshTokenDays = 30)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
        _refreshTokenDays = refreshTokenDays;
    }

    public async Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.GetByEmailGlobalAsync(normalizedEmail, ct) != null)
            return Result.Fail<LoginResponse>("Este email já está cadastrado. Faça login com sua conta existente.");

        var slug = GenerateSlug(request.TenantName);

        if (await _tenantRepository.SlugExistsAsync(slug, ct))
        {
            slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.TenantName.Trim(),
            Slug = slug,
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone,
            Plan = "free",
            Status = TenantStatus.Active,
            MaxUsers = 5,
            MaxInstagramAccounts = 1,
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow
        };

        await _tenantRepository.CreateAsync(tenant, ct);

        if (await _userRepository.EmailExistsAsync(request.Email, tenant.Id, ct))
        {
            return Result.Fail<LoginResponse>("Email já está em uso.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Phone = request.Phone,
            Status = UserStatus.Active,
            EmailConfirmed = false,
            CreatedAt = _dateTimeProvider.UtcNow,
            UpdatedAt = _dateTimeProvider.UtcNow
        };

        await _userRepository.CreateAsync(user, ct);

        // Assign Admin role
        var adminRole = await _roleRepository.GetByNameAsync("Admin", ct);
        if (adminRole != null)
        {
            await _roleRepository.AssignRoleToUserAsync(user.Id, adminRole.Id, ct);
        }

        // Generate tokens
        var roles = await _roleRepository.GetUserRoleNamesAsync(user.Id, ct);
        var permissions = await _permissionRepository.GetUserPermissionCodesAsync(user.Id, ct);

        var roleList = roles.ToList();
        var permissionList = permissions.ToList();

        var accessToken = _tokenService.GenerateAccessToken(user, roleList, permissionList);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = _dateTimeProvider.UtcNow.AddDays(_refreshTokenDays),
            CreatedAt = _dateTimeProvider.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(refreshToken, ct);

        await _auditLogRepository.CreateAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = user.Id,
            Action = "tenant.registered",
            EntityType = "Tenant",
            EntityId = tenant.Id,
            CreatedAt = _dateTimeProvider.UtcNow
        }, ct);

        return Result.Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = 3600,
            User = new UserInfo
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                Roles = roleList,
                Permissions = permissionList
            }
        });
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailGlobalAsync(request.Email.Trim().ToLowerInvariant(), ct);

        if (user == null)
            return Result.Fail<LoginResponse>("Credenciais inválidas.");

        if (user.Status != UserStatus.Active)
            return Result.Fail<LoginResponse>("Usuário inativo ou bloqueado.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Fail<LoginResponse>("Credenciais inválidas.");

        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, ct);
        if (tenant == null || tenant.Status != TenantStatus.Active)
            return Result.Fail<LoginResponse>("Empresa inativa ou suspensa.");

        var roles = (await _roleRepository.GetUserRoleNamesAsync(user.Id, ct)).ToList();
        var permissions = (await _permissionRepository.GetUserPermissionCodesAsync(user.Id, ct)).ToList();

        var accessToken = _tokenService.GenerateAccessToken(user, roles, permissions);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = _dateTimeProvider.UtcNow.AddDays(_refreshTokenDays),
            CreatedAt = _dateTimeProvider.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(refreshToken, ct);
        await _userRepository.UpdateLastLoginAsync(user.Id, _dateTimeProvider.UtcNow, ct);

        await _auditLogRepository.CreateAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            UserId = user.Id,
            Action = "user.login",
            EntityType = "User",
            EntityId = user.Id,
            CreatedAt = _dateTimeProvider.UtcNow
        }, ct);

        return Result.Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = 3600,
            User = new UserInfo
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                TenantId = user.TenantId,
                TenantName = tenant.Name,
                Roles = roles,
                Permissions = permissions
            }
        });
    }

    public async Task<Result<LoginResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);

        if (existingToken == null || !existingToken.IsActive)
            return Result.Fail<LoginResponse>("Token inválido ou expirado.");

        var user = await _userRepository.GetByIdAsync(existingToken.UserId, Guid.Empty, ct);
        if (user == null || user.Status != UserStatus.Active)
            return Result.Fail<LoginResponse>("Usuário não encontrado, inativo ou bloqueado.");

        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, ct);
        if (tenant == null || tenant.Status != TenantStatus.Active)
            return Result.Fail<LoginResponse>("Empresa inativa ou suspensa.");

        var roles = (await _roleRepository.GetUserRoleNamesAsync(user.Id, ct)).ToList();
        var permissions = (await _permissionRepository.GetUserPermissionCodesAsync(user.Id, ct)).ToList();

        var newAccessToken = _tokenService.GenerateAccessToken(user, roles, permissions);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

        // Revoke old token
        await _refreshTokenRepository.RevokeAsync(existingToken.Id, newRefreshTokenValue, ct);

        // Create new refresh token
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAt = _dateTimeProvider.UtcNow.AddDays(_refreshTokenDays),
            CreatedAt = _dateTimeProvider.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(newRefreshToken, ct);

        return Result.Ok(new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresIn = 3600,
            User = new UserInfo
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                TenantId = user.TenantId,
                TenantName = tenant.Name,
                Roles = roles,
                Permissions = permissions
            }
        });
    }

    public async Task<Result> LogoutAsync(Guid userId, CancellationToken ct = default)
    {
        await _refreshTokenRepository.RevokeAllByUserAsync(userId, ct);
        return Result.Ok();
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[áàâãä]", "a");
        slug = Regex.Replace(slug, @"[éèêë]", "e");
        slug = Regex.Replace(slug, @"[íìîï]", "i");
        slug = Regex.Replace(slug, @"[óòôõö]", "o");
        slug = Regex.Replace(slug, @"[úùûü]", "u");
        slug = Regex.Replace(slug, @"[ç]", "c");
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');
        return slug;
    }
}
// src/SocialAutomation.Application/Users/Services/UserService.cs

using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Application.Users.DTOs;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.Users.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UserService(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IRoleRepository roleRepository,
        IAuditLogRepository auditLogRepository,
        ITenantContext tenantContext,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _roleRepository = roleRepository;
        _auditLogRepository = auditLogRepository;
        _tenantContext = tenantContext;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PagedResult<UserResponse>>> GetPagedAsync(PagedRequest request, CancellationToken ct = default)
    {
        var (items, total) = await _userRepository.GetPagedAsync(
            _tenantContext.TenantId, request.Page, request.PageSize, request.Search, ct);

        var users = new List<UserResponse>();
        foreach (var user in items)
        {
            var roles = await _roleRepository.GetUserRoleNamesAsync(user.Id, ct);
            users.Add(new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status.ToString(),
                EmailConfirmed = user.EmailConfirmed,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList()
            });
        }

        return Result.Ok(new PagedResult<UserResponse>
        {
            Items = users,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }

    public async Task<Result<UserResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, _tenantContext.TenantId, ct);
        if (user == null)
            return Result.Fail<UserResponse>("Usuário não encontrado.");

        var roles = await _roleRepository.GetUserRoleNamesAsync(user.Id, ct);

        return Result.Ok(new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status.ToString(),
            EmailConfirmed = user.EmailConfirmed,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        });
    }

    public async Task<Result<Guid>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(_tenantContext.TenantId, ct);
        if (tenant == null)
            return Result.Fail<Guid>("Empresa não encontrada.");

        var currentCount = await _userRepository.CountByTenantAsync(_tenantContext.TenantId, ct);
        if (currentCount >= tenant.MaxUsers)
            return Result.Fail<Guid>($"Limite de {tenant.MaxUsers} usuários atingido.");

        if (await _userRepository.EmailExistsAsync(request.Email, _tenantContext.TenantId, ct))
            return Result.Fail<Guid>("Email já está em uso.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
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

        // Assign roles
        foreach (var roleName in request.Roles)
        {
            var role = await _roleRepository.GetByNameAsync(roleName, ct);
            if (role != null)
            {
                await _roleRepository.AssignRoleToUserAsync(user.Id, role.Id, ct);
            }
        }

        await _auditLogRepository.CreateAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            UserId = _tenantContext.UserId,
            Action = "user.created",
            EntityType = "User",
            EntityId = user.Id,
            CreatedAt = _dateTimeProvider.UtcNow
        }, ct);

        return Result.Ok(user.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, _tenantContext.TenantId, ct);
        if (user == null)
            return Result.Fail("Usuário não encontrado.");

        user.Name = request.Name.Trim();
        user.Phone = request.Phone;
        user.AvatarUrl = request.AvatarUrl;
        user.UpdatedAt = _dateTimeProvider.UtcNow;

        await _userRepository.UpdateAsync(user, ct);

        // Update roles
        await _roleRepository.RemoveAllUserRolesAsync(user.Id, ct);
        foreach (var roleName in request.Roles)
        {
            var role = await _roleRepository.GetByNameAsync(roleName, ct);
            if (role != null)
            {
                await _roleRepository.AssignRoleToUserAsync(user.Id, role.Id, ct);
            }
        }

        return Result.Ok();
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, _tenantContext.TenantId, ct);
        if (user == null)
            return Result.Fail("Usuário não encontrado.");

        if (user.Id == _tenantContext.UserId)
            return Result.Fail("Não é possível desativar seu próprio usuário.");

        user.Status = UserStatus.Inactive;
        user.UpdatedAt = _dateTimeProvider.UtcNow;
        await _userRepository.UpdateAsync(user, ct);

        return Result.Ok();
    }

    public async Task<Result> UpdateRolesAsync(Guid userId, UpdateUserRolesRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, _tenantContext.TenantId, ct);
        if (user == null)
            return Result.Fail("Usuário não encontrado.");

        await _roleRepository.RemoveAllUserRolesAsync(userId, ct);

        foreach (var roleId in request.RoleIds)
        {
            await _roleRepository.AssignRoleToUserAsync(userId, roleId, ct);
        }

        return Result.Ok();
    }
}
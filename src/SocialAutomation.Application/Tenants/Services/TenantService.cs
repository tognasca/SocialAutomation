// src/SocialAutomation.Application/Tenants/Services/TenantService.cs

using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Application.Tenants.DTOs;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.Tenants.Services;

public class TenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TenantService(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<TenantResponse>> GetCurrentAsync(CancellationToken ct = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(_tenantContext.TenantId, ct);
        if (tenant == null)
            return Result.Fail<TenantResponse>("Empresa não encontrada.");

        return Result.Ok(new TenantResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            Document = tenant.Document,
            Email = tenant.Email,
            Phone = tenant.Phone,
            Plan = tenant.Plan,
            Status = tenant.Status.ToString(),
            MaxUsers = tenant.MaxUsers,
            MaxInstagramAccounts = tenant.MaxInstagramAccounts,
            CreatedAt = tenant.CreatedAt
        });
    }

    public async Task<Result> UpdateCurrentAsync(UpdateTenantRequest request, CancellationToken ct = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(_tenantContext.TenantId, ct);
        if (tenant == null)
            return Result.Fail("Empresa não encontrada.");

        tenant.Name = request.Name.Trim();
        tenant.Document = request.Document;
        tenant.Email = request.Email;
        tenant.Phone = request.Phone;
        tenant.UpdatedAt = _dateTimeProvider.UtcNow;

        await _tenantRepository.UpdateAsync(tenant, ct);
        return Result.Ok();
    }
}
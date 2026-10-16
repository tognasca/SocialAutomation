// src/SocialAutomation.Application/Common/Interfaces/ITenantContext.cs

namespace SocialAutomation.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    void Set(Guid tenantId, Guid userId);
}
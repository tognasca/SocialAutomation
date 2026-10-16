
namespace SocialAutomation.Domain.Entities;

public class LeadActivity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid LeadId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}

namespace SocialAutomation.Domain.Entities;

public class LeadScoreRule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string EventTrigger { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
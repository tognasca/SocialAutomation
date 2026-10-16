namespace SocialAutomation.Domain.Entities;

public class AutomationFlow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public string TriggerConfig { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public int ExecutionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<AutomationNode> Nodes { get; set; } = new();
}

namespace SocialAutomation.Domain.Entities;

public class Tag
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366F1";
    public DateTime CreatedAt { get; set; }
}
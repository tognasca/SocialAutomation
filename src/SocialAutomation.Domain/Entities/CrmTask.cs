
namespace SocialAutomation.Domain.Entities;

public class CrmTask
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid LeadId { get; set; }
    public string? LeadName { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Pendente";
    public string Priority { get; set; } = "Media";
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
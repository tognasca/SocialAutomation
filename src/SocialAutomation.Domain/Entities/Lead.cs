
using SocialAutomation.Domain.Enums;

namespace SocialAutomation.Domain.Entities;

public class Lead
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? InstagramAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? InstagramUsername { get; set; }
    public string? InstagramUserId { get; set; }
    public string? ProfileImage { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string Source { get; set; } = "instagram_dm";
    public string Status { get; set; } = "Novo";
    public string PipelineStage { get; set; } = "Novo";
    public int PipelineOrder { get; set; }
    public decimal DealValue { get; set; }
    public Guid? OwnerUserId { get; set; }
    public int Score { get; set; }
    public string ScoreClassification { get; set; } = "Frio";
    public string? Notes { get; set; }
    public DateTime? LastInteractionAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation (Preenchido nas queries Dapper)
    public List<Tag> Tags { get; set; } = new();
    public string? OwnerName { get; set; }
}
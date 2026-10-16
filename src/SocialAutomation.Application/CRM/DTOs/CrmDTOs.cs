
namespace SocialAutomation.Application.CRM.DTOs;

public class LeadResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? InstagramUsername { get; set; }
    public string? ProfileImage { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PipelineStage { get; set; } = string.Empty;
    public decimal DealValue { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? OwnerName { get; set; }
    public int Score { get; set; }
    public string ScoreClassification { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? LastInteractionAt { get; set; }
    public DateTime? NextFollowUpAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TagDto> Tags { get; set; } = new();
}

public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class CreateLeadRequest
{
    public string Name { get; set; } = string.Empty;
    public string? InstagramUsername { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? Source { get; set; } = "Manual";
    public string? PipelineStage { get; set; } = "Novo";
    public decimal DealValue { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? Notes { get; set; }
    public List<Guid> TagIds { get; set; } = new();
}

public class UpdateLeadRequest
{
    public string Name { get; set; } = string.Empty;
    public string? InstagramUsername { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PipelineStage { get; set; } = string.Empty;
    public decimal DealValue { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? Notes { get; set; }
}

public class MoveLeadStageRequest
{
    public string Stage { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class PipelineColumnDto
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
    public List<LeadResponse> Leads { get; set; } = new();
}

public class CreateTaskRequest
{
    public Guid LeadId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public string Priority { get; set; } = "Media";
}

public class TaskResponse
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string? LeadName { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LeadActivityResponse
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string? UserName { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
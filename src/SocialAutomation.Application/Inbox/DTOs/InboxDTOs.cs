namespace SocialAutomation.Application.Inbox.DTOs;

public class ConversationDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string LeadName { get; set; } = string.Empty;
    public string? InstagramUsername { get; set; }
    public string? LeadProfileImage { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    public string? LastMessagePreview { get; set; }
    public DateTime LastMessageAt { get; set; }
    public string? ScoreClassification { get; set; }
}
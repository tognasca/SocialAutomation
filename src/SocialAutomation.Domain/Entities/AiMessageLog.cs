namespace SocialAutomation.Domain.Entities;

public class AiMessageLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ConversationId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? AgentId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string? Intent { get; set; }
    public bool ShouldHandoff { get; set; }
    public string? HandoffReason { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
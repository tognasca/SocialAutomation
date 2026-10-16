
namespace SocialAutomation.Domain.Entities;

public class IntegrationLog
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int? StatusCode { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
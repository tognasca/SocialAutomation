namespace SocialAutomation.Domain.Entities;

public class AiAgent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public string Personality { get; set; } = "profissional";
    public string Tone { get; set; } = "amigavel";
    public string? Objective { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyDescription { get; set; }
    public string? ProductsServices { get; set; }
    public string? PricingInfo { get; set; }
    public string? BusinessHours { get; set; }
    public string? Faq { get; set; }
    public string? Instructions { get; set; }
    public string? HandoffRules { get; set; }
    public string ModelProvider { get; set; } = "openai";
    public string ModelName { get; set; } = "gpt-4o-mini";
    public decimal Temperature { get; set; } = 0.40m;
    public int MaxTokens { get; set; } = 800;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
namespace SocialAutomation.Application.AI.Dtos;

public class AiAgentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public string Personality { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string? Objective { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyDescription { get; set; }
    public string? ProductsServices { get; set; }
    public string? PricingInfo { get; set; }
    public string? BusinessHours { get; set; }
    public string? Faq { get; set; }
    public string? Instructions { get; set; }
    public string? HandoffRules { get; set; }
    public string ModelProvider { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public decimal Temperature { get; set; }
    public int MaxTokens { get; set; }
}
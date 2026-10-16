public class AiDecisionResult
{
    public string Reply { get; set; } = string.Empty;
    public string Intent { get; set; } = "general";
    public bool ShouldHandoff { get; set; }
    public string? HandoffReason { get; set; }
    public List<string> SuggestedTags { get; set; } = new();
    public int ScoreDelta { get; set; }
    public string? LeadStatusSuggestion { get; set; }
}
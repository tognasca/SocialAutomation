namespace SocialAutomation.Domain.Entities;

public sealed record ProposalPlan(int MaxVehicles, string Name, decimal Price, string Terms);

public sealed class CampaignProposal
{
    public string CompanyName { get; init; } = "";
    public string PlanName { get; init; } = "";
    public decimal Price { get; init; }
    public string Terms { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public DateTime? AcceptedAt { get; init; }
}

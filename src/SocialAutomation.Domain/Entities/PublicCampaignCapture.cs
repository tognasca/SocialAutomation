namespace SocialAutomation.Domain.Entities;

public sealed class PublicCampaignCapture
{
    public string Name { get; init; } = "";
    public string CompanyName { get; init; } = "";
    public string? Caption { get; init; }
    public string[] Questions { get; init; } = [];
}

public sealed record PublicCampaignSlot(long Id, DateTime StartsAt);
public sealed record PublicCampaignDiagnosis(string Material, IEnumerable<PublicCampaignSlot> Slots, bool HasContact, bool HasProposalPlans, PublicCampaignSlot? BookedSlot);
public sealed record CampaignChannelMetrics(string Channel, int Visits, int Diagnoses, int Demos, int Accepted);
public sealed record PublicCampaignContact(string Name, string? Email, string? Phone);

namespace SocialAutomation.Domain.Entities;

public sealed record CampaignDemoSlot(long Id, DateTime StartsAt, DateTime? BookedAt);

public sealed record CampaignSalesSettings(string[] Questions, IEnumerable<CampaignDemoSlot> Slots);

namespace SocialAutomation.Domain.Entities
{
    public class Campaign
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? InstagramAccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;
        public string SegmentFilter { get; set; } = "{}";
        public string Status { get; set; } = "Rascunho";
        public DateTime? ScheduledAt { get; set; }
        public int TotalAudience { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
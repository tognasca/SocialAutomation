namespace SocialAutomation.Domain.Entities
{  
    public class CampaignRecipient
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid CampaignId { get; set; }
        public Guid LeadId { get; set; }
        public string Status { get; set; } = "Pendente";
        public string? ErrorMessage { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
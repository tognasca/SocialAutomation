namespace SocialAutomation.Domain.Models.Analytics;

public class DashboardSummary
{
    public int NewLeads { get; set; }
    public int Conversations { get; set; }
    public int HotLeads { get; set; }
    public int Opportunities { get; set; }
    public int Clients { get; set; }
    public int FollowUpsPending { get; set; }
    public int MessagesReceived { get; set; }
    public int MessagesSent { get; set; }
    public int AiReplies { get; set; }
    public int HumanHandoffs { get; set; }
    public int CampaignsSent { get; set; }
    public int ContentPublished { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal TotalPipelineValue { get; set; }
}
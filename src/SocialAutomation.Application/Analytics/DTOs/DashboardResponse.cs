using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Analytics.DTOs
{
    public class DashboardResponse
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string Period { get; set; } = string.Empty;

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

        public List<FunnelStageDto> Funnel { get; set; } = new();
        public List<TimeSeriesDto> TimeSeries { get; set; } = new();
        public List<SourceDto> Sources { get; set; } = new();
        public List<ScoreDto> ScoreDistribution { get; set; } = new();
    }
}

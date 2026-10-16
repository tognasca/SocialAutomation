using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Automation.DTOs
{
    public class CampaignDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalAudience { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Analytics.DTOs
{
    public class AnalyticsFilterRequest
    {
        public string Period { get; set; } = "30d"; // today | 7d | 30d | 90d | custom
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}

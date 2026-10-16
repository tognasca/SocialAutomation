using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Models.Analytics
{
    public class FunnelStageMetric
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }
}

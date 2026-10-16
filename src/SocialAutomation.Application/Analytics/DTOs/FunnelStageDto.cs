using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Analytics.DTOs
{
    public class FunnelStageDto
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

}

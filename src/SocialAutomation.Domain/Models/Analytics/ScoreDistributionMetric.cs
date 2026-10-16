using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Models.Analytics
{
    public class ScoreDistributionMetric
    {
        public string Classification { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

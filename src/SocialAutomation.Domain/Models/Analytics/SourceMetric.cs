using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Models.Analytics
{
    public class SourceMetric
    {
        public string Source { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

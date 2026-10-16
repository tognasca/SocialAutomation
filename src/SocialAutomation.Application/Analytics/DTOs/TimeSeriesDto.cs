using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Analytics.DTOs
{
    public class TimeSeriesDto
    {
        public string Date { get; set; } = string.Empty;
        public int Leads { get; set; }
        public int Conversations { get; set; }
        public int Messages { get; set; }
        public int Clients { get; set; }
    }

}

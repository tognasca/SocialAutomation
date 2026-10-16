using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Models.Analytics
{
    public class TimeSeriesPoint
    {
        public DateTime Date { get; set; }
        public int Leads { get; set; }
        public int Conversations { get; set; }
        public int Messages { get; set; }
        public int Clients { get; set; }
    }
}

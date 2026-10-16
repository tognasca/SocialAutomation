using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Analytics.DTOs
{
    public class ScoreDto
    {
        public string Classification { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

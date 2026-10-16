using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Automation.DTOs
{
    public class CreateCampaignRequest
    {
        public Guid? InstagramAccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;
        public List<Guid> TagIds { get; set; } = new();
        public string? StatusFilter { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Automation.DTOs
{
    public class CreateCampaignRequest
    {
        public Guid? InstagramAccountId { get; set; }
        public string? InstagramMediaId { get; set; }
        public string? CommentKeyword { get; set; }
        public string? ReelScript { get; set; }
        public string? SocialCaption { get; set; }
        public string? CaptureMaterial { get; set; }
        public string[]? CaptureQuestions { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;
        public List<Guid> TagIds { get; set; } = new();
        public string? StatusFilter { get; set; }
    }
}

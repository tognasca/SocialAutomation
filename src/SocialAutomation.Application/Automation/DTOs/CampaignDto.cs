using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Automation.DTOs
{
    public class CampaignDto
    {
        public Guid Id { get; set; }
        public string? InstagramMediaId { get; set; }
        public string? CommentKeyword { get; set; }
        public string? PublicCaptureTitle { get; set; }
        public string? PublicCaptureIntro { get; set; }
        public bool PublicCaptureEnabled { get; set; }
        public string? ReelScript { get; set; }
        public string? SocialCaption { get; set; }
        public string? CaptureMaterial { get; set; }
        public int QualifiedCount { get; set; }
        public int BookedCount { get; set; }
        public int AcceptedCount { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalAudience { get; set; }
        public int ReplyCount { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

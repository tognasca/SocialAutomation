using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Content.DTOs
{
    public class CreateContentItemRequest
    {
        public Guid? InstagramAccountId { get; set; }
        public string Type { get; set; } = "Post";
        public string Title { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public List<string> Hashtags { get; set; } = new();
        public string? Cta { get; set; }
        public List<string> MediaUrls { get; set; } = new();
        public string MediaFormat { get; set; } = "1:1";
        public DateTime? ScheduledAt { get; set; }
        public List<ReelSceneDto> Scenes { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Content.DTOs
{
    public class ContentItemDto
    {
        public Guid Id { get; set; }
        public Guid? InstagramAccountId { get; set; }
        public string Type { get; set; } = string.Empty; // Post, Story, Reel
        public string Title { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public List<string> Hashtags { get; set; } = new();
        public string? Cta { get; set; }
        public List<string> MediaUrls { get; set; } = new();
        public string MediaFormat { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ScheduledAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ReelSceneDto> Scenes { get; set; } = new();
    }
}

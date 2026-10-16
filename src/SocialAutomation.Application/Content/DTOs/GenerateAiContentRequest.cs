using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Content.DTOs
{
    public class GenerateAiContentRequest
    {
        public string Topic { get; set; } = string.Empty;
        public string? ProductOrService { get; set; }
        public string ContentType { get; set; } = "Post"; // Post, Reel, Story
        public string Tone { get; set; } = "persuasivo";
        public string TargetAudience { get; set; } = "geral";
    }
}

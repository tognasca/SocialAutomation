using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Content.DTOs
{
    public class GeneratedAiContentResponse
    {
        public string Title { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string Cta { get; set; } = string.Empty;
        public List<string> Hashtags { get; set; } = new();
        public string VisualSuggestion { get; set; } = string.Empty;
        public List<ReelSceneDto> ReelScenes { get; set; } = new();
    }
}

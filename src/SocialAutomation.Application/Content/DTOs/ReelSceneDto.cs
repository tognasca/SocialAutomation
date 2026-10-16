using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Content.DTOs
{
    public class ReelSceneDto
    {
        public int SceneOrder { get; set; }
        public string ScriptText { get; set; } = string.Empty;
        public string? VisualDescription { get; set; }
        public int DurationSeconds { get; set; }
    }
}

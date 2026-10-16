using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Models.AI
{
    public class AiCompletionResponse
    {
        public string Content { get; set; } = string.Empty;
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public string RawJson { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}

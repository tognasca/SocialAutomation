using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Models.AI
{
    public class AiCompletionRequest
    {
        public string Provider { get; set; } = "openai";
        public string Model { get; set; } = "gpt-4o-mini";
        public decimal Temperature { get; set; } = 0.4m;
        public int MaxTokens { get; set; } = 800;
        public List<AiChatMessage> Messages { get; set; } = new();
    }

}

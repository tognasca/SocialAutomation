using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Models.AI
{
    public class AiChatMessage
    {
        public string Role { get; set; } = "user"; // system | user | assistant
        public string Content { get; set; } = string.Empty;
    }
}

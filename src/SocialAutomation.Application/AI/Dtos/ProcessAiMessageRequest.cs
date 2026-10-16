using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.AI.Dtos
{
    public class ProcessAiMessageRequest
    {
        public Guid ConversationId { get; set; }
        public string IncomingMessage { get; set; } = string.Empty;
    }
}

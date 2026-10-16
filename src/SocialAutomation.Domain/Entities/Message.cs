using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Entities
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ConversationId { get; set; }
        public string SenderType { get; set; } = string.Empty;
        public Guid? SenderId { get; set; }
        public string? InstagramMessageId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? MediaUrl { get; set; }
        public string Status { get; set; } = "Sent";
        public DateTime CreatedAt { get; set; }
    }
}

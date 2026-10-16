using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Inbox.DTOs
{
    public class MessageDto
    {
        public Guid Id { get; set; }
        public string SenderType { get; set; } = string.Empty;
        public Guid? SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

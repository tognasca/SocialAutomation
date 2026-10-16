using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Instagram.DTOs
{
    public class SendInstagramMessageRequest
    {
        public Guid InstagramAccountId { get; set; }
        public string RecipientInstagramScopedId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}

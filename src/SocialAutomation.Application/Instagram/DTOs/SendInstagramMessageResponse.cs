using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Instagram.DTOs
{
    public class SendInstagramMessageResponse
    {
        public string MessageId { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
    }
}

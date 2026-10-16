using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Instagram.DTOs
{
    public class InstagramAccountResponse
    {
        public Guid Id { get; set; }
        public string InstagramBusinessId { get; set; } = string.Empty;
        public string PageName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool WebhookSubscribed { get; set; }
        public DateTime? TokenExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
}

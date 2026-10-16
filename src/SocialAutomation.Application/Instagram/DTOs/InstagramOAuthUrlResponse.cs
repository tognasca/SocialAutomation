using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Instagram.DTOs
{
    public class InstagramOAuthUrlResponse
    {
        public string AuthorizationUrl { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }
}

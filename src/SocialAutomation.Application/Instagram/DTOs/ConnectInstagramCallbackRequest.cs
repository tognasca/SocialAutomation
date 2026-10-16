using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Instagram.DTOs
{
    public class ConnectInstagramCallbackRequest
    {
        public string Code { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
    }
}

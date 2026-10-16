using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Automation.DTOs
{
    public class CreateAutomationFlowRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TriggerType { get; set; } = string.Empty; // 'LeadCriado', 'TagAdicionada', etc.
        public bool IsActive { get; set; } = true;
        public List<AutomationNodeDto> Nodes { get; set; } = new();
    }
}

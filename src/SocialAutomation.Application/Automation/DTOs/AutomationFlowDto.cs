using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Automation.DTOs
{
    public class AutomationFlowDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TriggerType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int ExecutionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AutomationNodeDto> Nodes { get; set; } = new();
    }
}

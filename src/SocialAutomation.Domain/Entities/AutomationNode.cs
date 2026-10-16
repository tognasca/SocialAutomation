using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Entities
{
    public class AutomationNode
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid FlowId { get; set; }
        public int StepOrder { get; set; }
        public string NodeType { get; set; } = string.Empty; // Condition, Action, Delay
        public string ActionOrCondition { get; set; } = string.Empty;
        public string Config { get; set; } = "{}";
        public DateTime CreatedAt { get; set; }
    }
}

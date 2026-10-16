using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.Automation.DTOs
{
    public class AutomationNodeDto
    {
        public int StepOrder { get; set; }
        public string NodeType { get; set; } = string.Empty; // Condition, Action, Delay
        public string ActionOrCondition { get; set; } = string.Empty;
        public string ConfigJson { get; set; } = "{}";
    }
}

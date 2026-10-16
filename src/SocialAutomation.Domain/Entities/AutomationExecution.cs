using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Entities
{
    public class AutomationExecution
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid FlowId { get; set; }
        public Guid LeadId { get; set; }
        public int CurrentStep { get; set; }
        public string Status { get; set; } = "Running";
        public DateTime? NextExecutionAt { get; set; }
        public string ExecutionLog { get; set; } = "[]";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

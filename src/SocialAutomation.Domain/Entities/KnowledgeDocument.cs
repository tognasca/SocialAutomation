using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Entities
{
    public class KnowledgeDocument
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid KnowledgeBaseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string SourceType { get; set; } = "manual";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

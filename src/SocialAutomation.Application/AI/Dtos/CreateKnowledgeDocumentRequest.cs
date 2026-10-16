using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Application.AI.Dtos
{
    public class CreateKnowledgeDocumentRequest
    {
        public Guid? KnowledgeBaseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string SourceType { get; set; } = "manual";
    }
}

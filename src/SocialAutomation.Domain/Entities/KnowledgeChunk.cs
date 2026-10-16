using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Entities
{
    public class KnowledgeChunk
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid DocumentId { get; set; }
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public string? EmbeddingJson { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

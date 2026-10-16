using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Entities
{
    public class ContentItem
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? InstagramAccountId { get; set; }
        public string Type { get; set; } = "Post"; // Post, Story, Reel
        public string Title { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public string[] Hashtags { get; set; } = Array.Empty<string>();
        public string? Cta { get; set; }
        public string[] MediaUrls { get; set; } = Array.Empty<string>();
        public string MediaFormat { get; set; } = "1:1"; // 1:1, 4:5, 9:16, 16:9
        public string Status { get; set; } = "Rascunho"; // Rascunho, Agendado, Processando, Publicado, Erro
        public DateTime? ScheduledAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? InstagramPublicationId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<ReelScene> Scenes { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Entities
{
    public class Conversation
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid LeadId { get; set; }
        public Guid? InstagramAccountId { get; set; }
        public Guid? AssignedUserId { get; set; }
        public string Mode { get; set; } = "HUMANO";
        public string Status { get; set; } = "Aberto";
        public int UnreadCount { get; set; }
        public string? LastMessagePreview { get; set; }
        public DateTime LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public string? LeadName { get; set; }
        public string? LeadProfileImage { get; set; }
        public string? InstagramUsername { get; set; }
        public string? AssignedUserName { get; set; }
        public string? ScoreClassification { get; set; }
    }
}

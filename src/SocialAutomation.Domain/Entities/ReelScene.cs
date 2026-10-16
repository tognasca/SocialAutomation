using System;
using System.Collections.Generic;
using System.Text;

namespace SocialAutomation.Domain.Entities
{
    public class ReelScene
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ContentItemId { get; set; }
        public int SceneOrder { get; set; }
        public string ScriptText { get; set; } = string.Empty;
        public string? VisualDescription { get; set; }
        public int DurationSeconds { get; set; } = 5;
        public DateTime CreatedAt { get; set; }
    }
}

using System;

namespace McpServer.Core.Models
{
    public class ContextChunk
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty; // e.g., wiki, smapi, game_schema
        public string SourceLocator { get; set; } = string.Empty; // URL or absolute path
        public string? SectionAnchor { get; set; }
        public string? GameVersion { get; set; }
        public string? ModName { get; set; }
        public int Tokens { get; set; }
        public double? Score { get; set; }
    }
}

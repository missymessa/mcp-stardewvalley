using System;

namespace McpServer.Core.Models
{
    public class IngestResult
    {
        public string PageTitle { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public DateTime LastFetched { get; set; } = DateTime.UtcNow;
    }
}

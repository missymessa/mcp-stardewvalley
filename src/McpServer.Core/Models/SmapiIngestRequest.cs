namespace McpServer.Core.Models
{
    public class SmapiIngestRequest
    {
        public string Repo { get; set; } = string.Empty; // e.g., "Pathoschild/SMAPI"
        public string? Branch { get; set; }
        public string? PathFilter { get; set; }
    }
}

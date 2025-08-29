using System.Collections.Generic;

namespace McpServer.Core.Models
{
    public class QueryRequest
    {
        public string Query { get; set; } = string.Empty;
        public int TopK { get; set; } = 10;
        public Dictionary<string, string>? Filters { get; set; }
    }
}

using System.Collections.Generic;

namespace McpServer.Core.Models
{
    public class QueryResponse
    {
        public List<ContextChunk> Results { get; set; } = new List<ContextChunk>();
    }
}

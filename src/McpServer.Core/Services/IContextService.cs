using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Core.Models;

namespace McpServer.Core.Services
{
    public interface IContextService
    {
        Task<IEnumerable<ContextChunk>> QueryAsync(string query, int topK, Dictionary<string, string>? filters = null, CancellationToken ct = default);
        Task SeedAsync(IEnumerable<ContextChunk> chunks, CancellationToken ct = default);
    }
}

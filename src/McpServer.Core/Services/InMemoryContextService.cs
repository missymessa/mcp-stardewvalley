using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Core.Models;

namespace McpServer.Core.Services
{
    public class InMemoryContextService : IContextService
    {
        private readonly List<ContextChunk> _chunks = new();

        public Task<IEnumerable<ContextChunk>> QueryAsync(string query, int topK, Dictionary<string, string>? filters = null, CancellationToken ct = default)
        {
            IEnumerable<ContextChunk> results = _chunks;

            if (!string.IsNullOrWhiteSpace(query))
            {
                results = _chunks.Where(c => c.Text.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            if (filters != null && filters.Any())
            {
                foreach (var kv in filters)
                {
                    var key = kv.Key.ToLowerInvariant();
                    var val = kv.Value;

                    results = results.Where(c =>
                        (key == "source_type" && string.Equals(c.SourceType, val, StringComparison.OrdinalIgnoreCase)) ||
                        (key == "mod_name" && string.Equals(c.ModName, val, StringComparison.OrdinalIgnoreCase)) ||
                        (key == "game_version" && string.Equals(c.GameVersion, val, StringComparison.OrdinalIgnoreCase))
                    );
                }
            }

            return Task.FromResult(results.Take(topK));
        }

        public Task SeedAsync(IEnumerable<ContextChunk> chunks, CancellationToken ct = default)
        {
            _chunks.AddRange(chunks);
            return Task.CompletedTask;
        }
    }
}

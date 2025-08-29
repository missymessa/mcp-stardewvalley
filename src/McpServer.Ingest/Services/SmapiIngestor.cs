using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using McpServer.Core.Models;
using McpServer.Processing.Chunking;
using McpServer.VectorStore;
using McpServer.Embeddings;

namespace McpServer.Ingest.Services
{
    public class SmapiIngestor
    {
        private readonly HttpClient _httpClient;
        private readonly IChunker _chunker;
        private readonly IEmbeddingsProvider _embeddings;
        private readonly IVectorStore _vectorStore;
        private readonly ILogger<SmapiIngestor> _logger;

        public SmapiIngestor(HttpClient httpClient, IChunker chunker, IEmbeddingsProvider embeddings, IVectorStore vectorStore, ILogger<SmapiIngestor> logger)
        {
            _httpClient = httpClient;
            _chunker = chunker;
            _embeddings = embeddings;
            _vectorStore = vectorStore;
            _logger = logger;

            // GitHub API requires a User-Agent
            try { _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("mcp-stardewvalley-ingestor"); } catch { }
        }

        public async Task<int> IngestFromGitHubAsync(string ownerRepo, string? branch = null, string? pathFilter = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(ownerRepo)) throw new ArgumentNullException(nameof(ownerRepo));
            var parts = ownerRepo.Split('/');
            if (parts.Length != 2) throw new ArgumentException("Repo should be in the form 'owner/repo'", nameof(ownerRepo));
            var owner = parts[0];
            var repo = parts[1];

            // find default branch if branch not provided
            if (string.IsNullOrEmpty(branch))
            {
                var repoInfo = await _httpClient.GetFromJsonAsync<JsonElement>($"https://api.github.com/repos/{owner}/{repo}", ct);
                if (repoInfo.TryGetProperty("default_branch", out var def)) branch = def.GetString();
            }
            branch ??= "main";

            // get tree
            var treeUrl = $"https://api.github.com/repos/{owner}/{repo}/git/trees/{branch}?recursive=1";
            var treeDoc = await _httpClient.GetFromJsonAsync<JsonElement>(treeUrl, ct);
            var files = new List<string>();
            if (treeDoc.TryGetProperty("tree", out var treeArray) && treeArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in treeArray.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "blob" && item.TryGetProperty("path", out var pathProp))
                    {
                        var path = pathProp.GetString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(pathFilter) && !path.StartsWith(pathFilter, StringComparison.OrdinalIgnoreCase)) continue;
                        if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        {
                            files.Add(path);
                        }
                    }
                }
            }

            var stored = 0;
            foreach (var path in files)
            {
                try
                {
                    var rawUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}";
                    var content = await _httpClient.GetStringAsync(rawUrl, ct);
                    if (string.IsNullOrWhiteSpace(content)) continue;

                    var chunks = _chunker.ChunkText(content, "smapi", $"https://github.com/{owner}/{repo}/blob/{branch}/{path}", null).ToList();
                    if (!chunks.Any()) continue;

                    var texts = chunks.Select(c => c.Text).ToList();
                    var vectors = await _embeddings.EmbedTextsAsync(texts, ct);
                    await _vectorStore.UpsertAsync(chunks, vectors, ct);

                    stored += chunks.Count;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to fetch or ingest {Path}", path);
                }
            }

            return stored;
        }
    }
}

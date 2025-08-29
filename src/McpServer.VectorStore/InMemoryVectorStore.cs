using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Core.Models;

namespace McpServer.VectorStore
{
    public class InMemoryVectorStore : IVectorStore
    {
        private readonly object _lock = new object();
        private readonly Dictionary<Guid, float[]> _vectors = new();
        private readonly Dictionary<Guid, ContextChunk> _meta = new();

        public Task<int> CountAsync(CancellationToken ct = default)
        {
            lock (_lock) return Task.FromResult(_vectors.Count);
        }

        public Task UpsertAsync(IEnumerable<ContextChunk> chunks, IEnumerable<float[]> embeddings, CancellationToken ct = default)
        {
            var chunkList = chunks.ToList();
            var vecList = embeddings.ToList();
            if (chunkList.Count != vecList.Count) throw new ArgumentException("chunks and embeddings must have same length");

            lock (_lock)
            {
                for (int i = 0; i < chunkList.Count; i++)
                {
                    var id = chunkList[i].Id;
                    _meta[id] = chunkList[i];
                    _vectors[id] = vecList[i];
                }
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<(ContextChunk chunk, double score)>> QueryAsync(float[] queryEmbedding, int topK = 10, CancellationToken ct = default)
        {
            if (queryEmbedding == null || queryEmbedding.Length == 0) return Task.FromResult(Enumerable.Empty<(ContextChunk, double)>());

            List<(ContextChunk chunk, double score)> results = new();
            lock (_lock)
            {
                foreach (var kv in _vectors)
                {
                    var id = kv.Key;
                    var vec = kv.Value;
                    var score = CosineSimilarity(queryEmbedding, vec);
                    if (_meta.TryGetValue(id, out var chunk))
                    {
                        results.Add((chunk, score));
                    }
                }
            }

            var top = results.OrderByDescending(r => r.score).Take(topK);
            return Task.FromResult(top);
        }

        private static double CosineSimilarity(float[] a, float[] b)
        {
            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }
            if (na == 0 || nb == 0) return 0;
            return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        }
    }
}

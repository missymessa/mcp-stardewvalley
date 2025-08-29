using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Core.Models;

namespace McpServer.VectorStore
{
    public interface IVectorStore
    {
        Task UpsertAsync(IEnumerable<ContextChunk> chunks, IEnumerable<float[]> embeddings, CancellationToken ct = default);
        Task<IEnumerable<(ContextChunk chunk, double score)>> QueryAsync(float[] queryEmbedding, int topK = 10, CancellationToken ct = default);
        Task<int> CountAsync(CancellationToken ct = default);
    }
}

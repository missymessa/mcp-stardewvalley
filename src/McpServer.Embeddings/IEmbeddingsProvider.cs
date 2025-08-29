using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Embeddings
{
    public interface IEmbeddingsProvider
    {
        int Dimension { get; }
        Task<float[]> EmbedTextAsync(string text, CancellationToken ct = default);
        Task<float[][]> EmbedTextsAsync(IEnumerable<string> texts, CancellationToken ct = default);
    }
}

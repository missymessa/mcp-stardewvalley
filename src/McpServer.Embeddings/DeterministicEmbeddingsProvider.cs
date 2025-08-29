using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Embeddings
{
    // Simple deterministic embedding generator for local development and tests.
    public class DeterministicEmbeddingsProvider : IEmbeddingsProvider
    {
        private readonly int _dimension;

        public int Dimension => _dimension;

        public DeterministicEmbeddingsProvider(int dimension = 128)
        {
            if (dimension <= 0) throw new ArgumentOutOfRangeException(nameof(dimension));
            _dimension = dimension;
        }

        public Task<float[]> EmbedTextAsync(string text, CancellationToken ct = default)
        {
            var vector = GenerateDeterministicVector(text, _dimension);
            return Task.FromResult(vector);
        }

        public Task<float[][]> EmbedTextsAsync(IEnumerable<string> texts, CancellationToken ct = default)
        {
            var list = texts.Select(t => GenerateDeterministicVector(t, _dimension)).ToArray();
            return Task.FromResult(list);
        }

        private static float[] GenerateDeterministicVector(string text, int dim)
        {
            // Use SHA256 hash to seed a pseudo-random generator deterministically
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));
            var seed = BitConverter.ToInt32(bytes, 0);
            var rnd = new Random(seed);
            var v = new float[dim];
            for (int i = 0; i < dim; i++)
            {
                // value in range [-1,1)
                v[i] = (float)(rnd.NextDouble() * 2.0 - 1.0);
            }
            return v;
        }
    }
}

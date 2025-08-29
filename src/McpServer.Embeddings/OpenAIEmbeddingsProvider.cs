using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Embeddings
{
    // Adapter for the OpenAI embeddings API. Reads API key from the client headers or env var.
    public class OpenAIEmbeddingsProvider : IEmbeddingsProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private int _dimension;

        public int Dimension => _dimension;

        public OpenAIEmbeddingsProvider(HttpClient httpClient, string model = "text-embedding-3-small")
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _model = model;
            _dimension = 0;
        }

        public async Task<float[]> EmbedTextAsync(string text, CancellationToken ct = default)
        {
            var doc = await PostEmbeddingsRequestAsync(new[] { text }, ct);
            var first = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");
            var vector = first.EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
            _dimension = vector.Length;
            return vector;
        }

        public async Task<float[][]> EmbedTextsAsync(IEnumerable<string> texts, CancellationToken ct = default)
        {
            var inputs = texts as string[] ?? texts.ToArray();
            var doc = await PostEmbeddingsRequestAsync(inputs, ct);
            var arr = doc.RootElement.GetProperty("data").EnumerateArray().ToArray();
            var result = new float[arr.Length][];
            for (int i = 0; i < arr.Length; i++)
            {
                var emb = arr[i].GetProperty("embedding").EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
                result[i] = emb;
            }
            if (result.Length > 0) _dimension = result[0].Length;
            return result;
        }

        private async Task<JsonDocument> PostEmbeddingsRequestAsync(string[] inputs, CancellationToken ct)
        {
            var payload = new { model = _model, input = inputs };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _httpClient.PostAsync("v1/embeddings", content, ct);
            var str = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Embedding request failed: {resp.StatusCode}: {str}");
            }

            return JsonDocument.Parse(str);
        }
    }
}

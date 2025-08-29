using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using McpServer.Embeddings;
using McpServer.Core.Models;

namespace McpServer.Embeddings
{
    [McpServerToolType]
    public static class EmbeddingsTool
    {
        [McpServerTool(Name = "embeddings")]
        [Description("Return embeddings for an array of input texts")]
        public static async Task<CallToolResult> GetEmbeddingsAsync(
            IEmbeddingsProvider embeddingsProvider,
            string[] inputs,
            CancellationToken cancellationToken)
        {
            // Compute embeddings via DI-provided provider
            var vectors = await embeddingsProvider.EmbedTextsAsync(inputs, cancellationToken);

            // Return results as a JSON content block; tool consumers should parse it.
            var payload = new { embeddings = vectors };
            var json = JsonSerializer.Serialize(payload);

            var content = new List<ContentBlock>
            {
                new TextContentBlock { Text = json, Type = "json" }
            };

            return new CallToolResult { Content = content };
        }
    }
}

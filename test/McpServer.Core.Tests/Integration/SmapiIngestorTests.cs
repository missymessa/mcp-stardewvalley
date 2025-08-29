using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using McpServer.Ingest.Services;
using McpServer.Processing.Chunking;
using McpServer.Embeddings;
using McpServer.VectorStore;

namespace McpServer.Core.Tests.Integration
{
    [Category("Integration")]
    [TestFixture]
    public class SmapiIngestorTests
    {
        private class FakeHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var url = request.RequestUri?.ToString() ?? string.Empty;
                if (url.Contains("/repos/Pathoschild/SMAPI/git/trees"))
                {
                    var json = JsonSerializer.Serialize(new { tree = new[] { new { path = "README.md", type = "blob" }, new { path = "src/Foo.cs", type = "blob" } } });
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
                }
                if (url.Contains("/repos/Pathoschild/SMAPI"))
                {
                    var json = JsonSerializer.Serialize(new { default_branch = "main" });
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
                }
                if (url.Contains("raw.githubusercontent.com") && url.EndsWith("README.md"))
                {
                    var text = "Parsnip: base sell price 35g; grows in spring;";
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text) });
                }
                if (url.Contains("raw.githubusercontent.com") && url.EndsWith("src/Foo.cs"))
                {
                    var text = "/// <summary>Foo helper</summary> public class Foo { public void Bar() {} }";
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text) });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public async Task SmapiIngestor_IngestsRepoAndStoresChunks()
        {
            var handler = new FakeHandler();
            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("test-agent");

            var chunker = new SimpleChunker(40, 5);
            var embeddings = new DeterministicEmbeddingsProvider(32);
            var store = new McpServer.VectorStore.InMemoryVectorStore();
            var logger = NullLogger<SmapiIngestor>.Instance;

            var ingestor = new SmapiIngestor(httpClient, chunker, embeddings, store, logger);

            var stored = await ingestor.IngestFromGitHubAsync("Pathoschild/SMAPI", "main", null);
            Assert.Greater(stored, 0);

            var count = await store.CountAsync();
            Assert.Greater(count, 0);
        }
    }
}

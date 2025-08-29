using System;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using McpServer.Ingest.Services;
using McpServer.Core.Models;
using McpServer.Embeddings;

namespace McpServer.Core.Tests.Integration
{
    [Category("Integration")]
    [TestFixture]
    public class IngestStoreSearchTests
    {
        [Test]
        public async Task IngestStoreAndSearch_ReturnsExpectedChunk()
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace IMediaWikiIngestor with a test double
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMediaWikiIngestor));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddSingleton<IMediaWikiIngestor>(new TestMediaWikiIngestor());

                    // Ensure deterministic embeddings
                    var eDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IEmbeddingsProvider));
                    if (eDesc != null) services.Remove(eDesc);
                    services.AddSingleton<IEmbeddingsProvider>(sp => new DeterministicEmbeddingsProvider(64));
                });
            });

            var client = factory.CreateClient();

            // Ingest page
            var ingestResp = await client.PostAsJsonAsync("/v1/ingest/wiki/store", new IngestRequest { PageTitle = "Parsnip" });
            ingestResp.EnsureSuccessStatusCode();
            var stored = await ingestResp.Content.ReadFromJsonAsync<JsonElement>();

            Assert.IsNotNull(stored);

            // Search
            var searchResp = await client.PostAsJsonAsync("/v1/context/search", new { query = "parsnip sell price", topK = 5 });
            searchResp.EnsureSuccessStatusCode();

            var json = await JsonDocument.ParseAsync(await searchResp.Content.ReadAsStreamAsync());
            var results = json.RootElement.GetProperty("results");

            Assert.IsTrue(results.GetArrayLength() > 0);
            var topText = results[0].GetProperty("text").GetString();
            Assert.IsTrue(topText != null && topText.Contains("Parsnip", StringComparison.OrdinalIgnoreCase));
        }

        private class TestMediaWikiIngestor : IMediaWikiIngestor
        {
            public Task<IngestResult> FetchPageAsync(string pageTitle, System.Threading.CancellationToken ct = default)
            {
                var text = pageTitle switch
                {
                    "Parsnip" => "Parsnip: base sell price 35g; grows in spring;",
                    "Cauliflower" => "Cauliflower: base sell price 80g; grows in summer;",
                    _ => string.Empty
                };

                return Task.FromResult(new IngestResult
                {
                    PageTitle = pageTitle,
                    Text = text,
                    SourceUrl = $"https://stardewvalley.fandom.com/wiki/{Uri.EscapeDataString(pageTitle)}",
                    LastFetched = DateTime.UtcNow
                });
            }
        }
    }
}

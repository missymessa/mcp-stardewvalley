using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using McpServer.Ingest.Services;
using McpServer.Processing.Chunking;
using McpServer.Embeddings;
using McpServer.VectorStore;
using McpServer.Core.Models;

namespace McpServer.Benchmark
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var pagesFile = args.Length > 0 ? args[0] : Path.Combine("benchmarks", "seed_pages.txt");
            var queriesFile = args.Length > 1 ? args[1] : Path.Combine("benchmarks", "queries.json");

            var services = new ServiceCollection();

            // Register services (simple local setup)
            services.AddHttpClient<IMediaWikiIngestor, MediaWikiIngestor>(c =>
            {
                c.BaseAddress = new Uri("https://stardewvalley.fandom.com/");
                c.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddSingleton<IChunker, SimpleChunker>();

            // Use deterministic embeddings for local benchmark by default
            services.AddSingleton<IEmbeddingsProvider>(sp => new DeterministicEmbeddingsProvider(128));

            services.AddSingleton<IVectorStore, InMemoryVectorStore>();

            var sp = services.BuildServiceProvider();
            var ingestor = sp.GetRequiredService<IMediaWikiIngestor>();
            var chunker = sp.GetRequiredService<IChunker>();
            var embeddings = sp.GetRequiredService<IEmbeddingsProvider>();
            var store = sp.GetRequiredService<IVectorStore>();

            // ingest pages
            if (!File.Exists(pagesFile))
            {
                Console.WriteLine($"Pages file '{pagesFile}' not found.");
                return 1;
            }

            var pages = File.ReadAllLines(pagesFile).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList();
            Console.WriteLine($"Seeding {pages.Count} pages...");

            foreach (var p in pages)
            {
                Console.WriteLine($"Ingesting: {p}");
                var result = await ingestor.FetchPageAsync(p);
                if (string.IsNullOrWhiteSpace(result?.Text))
                {
                    Console.WriteLine($"  skipped: no text for {p}");
                    continue;
                }

                var chunks = chunker.ChunkText(result.Text, "wiki", result.SourceUrl, null).ToList();
                var texts = chunks.Select(c => c.Text).ToList();
                var vectors = await embeddings.EmbedTextsAsync(texts);
                await store.UpsertAsync(chunks, vectors);
                Console.WriteLine($"  stored {chunks.Count} chunks\n");
            }

            // run queries
            if (!File.Exists(queriesFile))
            {
                Console.WriteLine($"Queries file '{queriesFile}' not found.");
                return 1;
            }

            var qJson = await File.ReadAllTextAsync(queriesFile);
            var queries = JsonSerializer.Deserialize<List<QuerySpec>>(qJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<QuerySpec>();

            int hits = 0;

            foreach (var q in queries)
            {
                var qVec = await embeddings.EmbedTextAsync(q.Query);
                var results = (await store.QueryAsync(qVec, 5)).ToList();
                static string TitleFromUrl(string? src)
                {
                    if (string.IsNullOrWhiteSpace(src)) return string.Empty;
                    try
                    {
                        var u = new Uri(src);
                        var seg = u.Segments.LastOrDefault()?.Trim('/');
                        return Uri.UnescapeDataString(seg ?? string.Empty).Replace('_', ' ');
                    }
                    catch
                    {
                        return src ?? string.Empty;
                    }
                }

                var found = results.Any(r => string.Equals(TitleFromUrl(r.chunk.SourceLocator), q.Expected, StringComparison.OrdinalIgnoreCase));
                if (found) hits++;
                Console.WriteLine($"Query: '{q.Query}' Expected: '{q.Expected}' Found: {found}");
                var top = results.FirstOrDefault();
                if (top.chunk != null) Console.WriteLine($"  Top1: {TitleFromUrl(top.chunk.SourceLocator)} (score={top.score:F3})\n");
                else Console.WriteLine("  No results\n");
            }

            Console.WriteLine($"Top-5 hit rate: {hits}/{queries.Count} = {(queries.Count > 0 ? ((double)hits / queries.Count).ToString("P2") : "0%")}");
            return 0;
        }

        private class QuerySpec
        {
            public string Query { get; set; } = string.Empty;
            public string Expected { get; set; } = string.Empty;
        }
    }
}

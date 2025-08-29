using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using McpServer.Core.Models;

namespace McpServer.Ingest.Services
{
    public class MediaWikiIngestor : IMediaWikiIngestor
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MediaWikiIngestor> _logger;

        public MediaWikiIngestor(HttpClient httpClient, ILogger<MediaWikiIngestor> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IngestResult> FetchPageAsync(string pageTitle, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(pageTitle))
            {
                return new IngestResult { PageTitle = pageTitle ?? string.Empty, Text = string.Empty, SourceUrl = string.Empty, LastFetched = DateTime.UtcNow };
            }

            var url = $"https://stardewvalley.fandom.com/api.php?action=query&prop=extracts&explaintext=1&format=json&titles={Uri.EscapeDataString(pageTitle)}&redirects=1";

            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (doc.RootElement.TryGetProperty("query", out var query) && query.TryGetProperty("pages", out var pages))
            {
                var first = pages.EnumerateObject().FirstOrDefault();
                if (first.Value.ValueKind != JsonValueKind.Null)
                {
                    var page = first.Value;
                    var extract = page.TryGetProperty("extract", out var ext) ? ext.GetString() ?? string.Empty : string.Empty;
                    var title = page.TryGetProperty("title", out var t) ? t.GetString() ?? pageTitle : pageTitle;
                    var src = $"https://stardewvalley.fandom.com/wiki/{Uri.EscapeDataString(title)}";

                    return new IngestResult
                    {
                        PageTitle = title,
                        Text = extract,
                        SourceUrl = src,
                        LastFetched = DateTime.UtcNow
                    };
                }
            }

            return new IngestResult { PageTitle = pageTitle, Text = string.Empty, SourceUrl = string.Empty, LastFetched = DateTime.UtcNow };
        }
    }
}

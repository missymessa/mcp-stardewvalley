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
            // Ensure a sensible User-Agent is sent; some wiki hosts reject requests without one
            try
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Clear();
                _httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("MCP-StardewValley", "0.1"));
            }
            catch
            {
                // ignore if header cannot be set
            }
        }

        public async Task<IngestResult> FetchPageAsync(string pageTitle, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(pageTitle))
            {
                return new IngestResult { PageTitle = pageTitle ?? string.Empty, Text = string.Empty, SourceUrl = string.Empty, LastFetched = DateTime.UtcNow };
            }

            var url = $"https://stardewvalley.fandom.com/api.php?action=query&prop=extracts&explaintext=1&format=json&formatversion=2&titles={Uri.EscapeDataString(pageTitle)}&redirects=1";

            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (doc.RootElement.TryGetProperty("query", out var query) && query.TryGetProperty("pages", out var pages))
            {
                JsonElement pageElement = default;

                if (pages.ValueKind == JsonValueKind.Object)
                {
                    var first = pages.EnumerateObject().FirstOrDefault();
                    if (first.Value.ValueKind != JsonValueKind.Null) pageElement = first.Value;
                }
                else if (pages.ValueKind == JsonValueKind.Array)
                {
                    if (pages.GetArrayLength() > 0) pageElement = pages[0];
                }

                if (pageElement.ValueKind != JsonValueKind.Undefined && pageElement.ValueKind != JsonValueKind.Null)
                {
                    var extract = pageElement.TryGetProperty("extract", out var ext) ? ext.GetString() ?? string.Empty : string.Empty;
                    var title = pageElement.TryGetProperty("title", out var t) ? t.GetString() ?? pageTitle : pageTitle;
                    var src = $"https://stardewvalley.fandom.com/wiki/{Uri.EscapeDataString(title)}";

                    if (!string.IsNullOrWhiteSpace(extract))
                    {
                        return new IngestResult
                        {
                            PageTitle = title,
                            Text = extract,
                            SourceUrl = src,
                            LastFetched = DateTime.UtcNow
                        };
                    }
                    else
                    {
                        // fallback to parse -> text (HTML) and strip tags
                        try
                        {
                            var parseUrl = $"https://stardewvalley.fandom.com/api.php?action=parse&page={Uri.EscapeDataString(title)}&format=json&prop=text";
                            var parseResp = await _httpClient.GetAsync(parseUrl, ct);
                            parseResp.EnsureSuccessStatusCode();
                            using var parseStream = await parseResp.Content.ReadAsStreamAsync(ct);
                            using var parseDoc = await JsonDocument.ParseAsync(parseStream, cancellationToken: ct);
                            if (parseDoc.RootElement.TryGetProperty("parse", out var parse) && parse.TryGetProperty("text", out var textNode))
                            {
                                if (textNode.TryGetProperty("*", out var htmlNode))
                                {
                                    var html = htmlNode.GetString() ?? string.Empty;
                                    var plain = StripHtml(html);
                                    if (!string.IsNullOrWhiteSpace(plain))
                                    {
                                        return new IngestResult
                                        {
                                            PageTitle = title,
                                            Text = plain,
                                            SourceUrl = src,
                                            LastFetched = DateTime.UtcNow
                                        };
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Parse fallback failed for {Title}", title);
                        }
                    }
                }
            }

            return new IngestResult { PageTitle = pageTitle, Text = string.Empty, SourceUrl = string.Empty, LastFetched = DateTime.UtcNow };
        }

        private static string StripHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // quick-and-dirty html tag remover
            var sb = new System.Text.StringBuilder(input.Length);
            bool inside = false;
            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];
                if (ch == '<') inside = true;
                else if (ch == '>') { inside = false; continue; }
                if (!inside) sb.Append(ch);
            }
            return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "\\s+", " ").Trim();
        }
    }
}

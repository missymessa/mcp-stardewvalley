using McpServer.Core.Models;
using McpServer.Core.Services;
using McpServer.Ingest.Services;
using McpServer.Processing.Chunking;
using McpServer.Embeddings;
using McpServer.VectorStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<IContextService, InMemoryContextService>();
builder.Services.AddHttpClient<IMediaWikiIngestor, MediaWikiIngestor>(client =>
{
    client.BaseAddress = new Uri("https://stardewvalley.fandom.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Processing & embeddings
builder.Services.AddSingleton<IChunker, SimpleChunker>();

// Register a deterministic local embeddings provider by default
builder.Services.AddSingleton<IEmbeddingsProvider>(sp => new DeterministicEmbeddingsProvider(128));

// Vector store
builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/v1/context/query", async (QueryRequest request, IContextService contextService) =>
{
    var results = await contextService.QueryAsync(request.Query, request.TopK, request.Filters);
    return Results.Ok(new QueryResponse { Results = results.ToList() });
});

app.MapPost("/v1/ingest/wiki", async (IngestRequest request, IMediaWikiIngestor ingestor) =>
{
    if (string.IsNullOrWhiteSpace(request.PageTitle) && string.IsNullOrWhiteSpace(request.Url))
    {
        return Results.BadRequest(new { error = "Provide pageTitle or url" });
    }

    var title = request.PageTitle;
    if (string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(request.Url))
    {
        try
        {
            var uri = new Uri(request.Url);
            title = uri.Segments.Last();
            title = Uri.UnescapeDataString(title).Replace('_', ' ');
        }
        catch
        {
            return Results.BadRequest(new { error = "Invalid url" });
        }
    }

    var result = await ingestor.FetchPageAsync(title!);
    return Results.Ok(result);
});

app.MapPost("/v1/ingest/wiki/preview", async (IngestRequest request, IMediaWikiIngestor ingestor, IChunker chunker, IEmbeddingsProvider embeddings) =>
{
    if (string.IsNullOrWhiteSpace(request.PageTitle) && string.IsNullOrWhiteSpace(request.Url))
    {
        return Results.BadRequest(new { error = "Provide pageTitle or url" });
    }

    var title = request.PageTitle;
    if (string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(request.Url))
    {
        try
        {
            var uri = new Uri(request.Url);
            title = uri.Segments.Last();
            title = Uri.UnescapeDataString(title).Replace('_', ' ');
        }
        catch
        {
            return Results.BadRequest(new { error = "Invalid url" });
        }
    }

    var ingestResult = await ingestor.FetchPageAsync(title!);
    if (string.IsNullOrWhiteSpace(ingestResult?.Text))
    {
        return Results.NotFound(new { error = "Page not found or no text available" });
    }

    var chunks = chunker.ChunkText(ingestResult.Text, "wiki", ingestResult.SourceUrl, null).ToList();
    if (!chunks.Any()) return Results.Ok(new { chunks = Array.Empty<object>() });

    var texts = chunks.Select(c => c.Text).ToList();
    var vectors = await embeddings.EmbedTextsAsync(texts);

    var result = chunks.Select((c, i) => new
    {
        id = c.Id,
        tokens = c.Tokens,
        text = c.Text,
        embedding = vectors.ElementAtOrDefault(i) ?? Array.Empty<float>()
    });

    return Results.Ok(new { pageTitle = ingestResult.PageTitle, chunks = result });
});

app.MapPost("/v1/ingest/wiki/store", async (IngestRequest request, IMediaWikiIngestor ingestor, IChunker chunker, IEmbeddingsProvider embeddings, IVectorStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.PageTitle) && string.IsNullOrWhiteSpace(request.Url))
    {
        return Results.BadRequest(new { error = "Provide pageTitle or url" });
    }

    var title = request.PageTitle;
    if (string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(request.Url))
    {
        try
        {
            var uri = new Uri(request.Url);
            title = uri.Segments.Last();
            title = Uri.UnescapeDataString(title).Replace('_', ' ');
        }
        catch
        {
            return Results.BadRequest(new { error = "Invalid url" });
        }
    }

    var ingestResult = await ingestor.FetchPageAsync(title!);
    if (string.IsNullOrWhiteSpace(ingestResult?.Text))
    {
        return Results.NotFound(new { error = "Page not found or no text available" });
    }

    var chunks = chunker.ChunkText(ingestResult.Text, "wiki", ingestResult.SourceUrl, null).ToList();
    if (!chunks.Any()) return Results.Ok(new { stored = 0 });

    var texts = chunks.Select(c => c.Text).ToList();
    var vectors = await embeddings.EmbedTextsAsync(texts);

    await store.UpsertAsync(chunks, vectors);

    return Results.Ok(new { stored = chunks.Count });
});

app.MapPost("/v1/context/search", async (QueryRequest request, IEmbeddingsProvider embeddings, IVectorStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Query)) return Results.BadRequest(new { error = "query is required" });

    var q = request.Query;
    var qVec = await embeddings.EmbedTextAsync(q);
    var k = Math.Max(1, request.TopK);
    var hits = await store.QueryAsync(qVec, k);

    var response = hits.Select(h => new
    {
        id = h.chunk.Id,
        score = h.score,
        text = h.chunk.Text,
        source = h.chunk.SourceLocator
    });

    return Results.Ok(new { query = q, results = response });
});

app.MapMcp();

// Seed initial sample content for local development
var contextSvc = app.Services.GetRequiredService<IContextService>();
await contextSvc.SeedAsync(new[]
{
    new ContextChunk
    {
        Id = Guid.NewGuid(),
        Text = "Parsnip: base sell price 35g; grows in spring; harvested after X days; related wiki entry: https://stardewvalleywiki.example/Parsnip",
        SourceType = "wiki",
        SourceLocator = "https://stardewvalleywiki.example/Parsnip",
        SectionAnchor = "Growth",
        GameVersion = "1.5",
        ModName = null,
        Tokens = 20,
        Score = null
    }
});

app.Run();

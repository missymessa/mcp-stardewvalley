using McpServer.Core.Models;
using McpServer.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<IContextService, InMemoryContextService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

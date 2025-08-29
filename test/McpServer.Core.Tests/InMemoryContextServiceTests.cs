using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using McpServer.Core.Models;
using McpServer.Core.Services;

namespace McpServer.Core.Tests
{
    [TestFixture]
    public class InMemoryContextServiceTests
    {
        [Test]
        public async Task Seed_Then_Query_Returns_Chunk()
        {
            var svc = new InMemoryContextService();
            var chunk = new ContextChunk
            {
                Id = Guid.NewGuid(),
                Text = "Parsnip: base sell price 35g; grows in spring;",
                SourceType = "wiki",
                SourceLocator = "https://stardewvalleywiki.example/Parsnip",
                Tokens = 10
            };

            await svc.SeedAsync(new[] { chunk });

            var results = (await svc.QueryAsync("parsnip", 5)).ToList();

            // NUnit assertions
            Assert.IsNotEmpty(results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(chunk.Id, results[0].Id);

            // Optional: use AwesomeAssertions for more expressive assertions (package referenced).
            // Example (if the package exposes an extension method): results.ShouldContain(r => r.Id == chunk.Id);
        }

        [Test]
        public async Task Query_With_NoMatch_Returns_Empty()
        {
            var svc = new InMemoryContextService();

            var results = (await svc.QueryAsync("nonexistent", 5)).ToList();
            Assert.IsEmpty(results);

            // Optional AwesomeAssertions example: results.ShouldBeEmpty();
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using McpServer.Embeddings;
using NUnit.Framework;

namespace McpServer.Core.Tests
{
    [TestFixture]
    public class DeterministicEmbeddingsProviderTests
    {
        [Test]
        public async Task EmbedText_ReturnsSameOnRepeatedCalls()
        {
            var provider = new DeterministicEmbeddingsProvider(32);
            var v1 = await provider.EmbedTextAsync("hello world");
            var v2 = await provider.EmbedTextAsync("hello world");
            Assert.IsTrue(v1.SequenceEqual(v2));
            Assert.AreEqual(32, v1.Length);
        }

        [Test]
        public async Task EmbedTexts_ReturnsConsistentResults()
        {
            var provider = new DeterministicEmbeddingsProvider(16);
            var texts = new[] { "a", "b", "a" };
            var batch = await provider.EmbedTextsAsync(texts);
            Assert.AreEqual(3, batch.Length);
            Assert.IsTrue(batch[0].SequenceEqual(batch[2]));
            Assert.IsFalse(batch[0].SequenceEqual(batch[1]));
        }
    }
}

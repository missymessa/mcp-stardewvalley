using System.Linq;
using McpServer.Processing.Chunking;
using NUnit.Framework;

namespace McpServer.Core.Tests
{
    [TestFixture]
    public class SimpleChunkerTests
    {
        [Test]
        public void ChunkText_SplitsIntoChunksWithOverlap()
        {
            var text = "one two three four five six seven eight nine ten";
            var chunker = new SimpleChunker(chunkSize: 4, overlap: 1);
            var chunks = chunker.ChunkText(text, "wiki", "src").ToList();

            Assert.AreEqual(3, chunks.Count);
            Assert.AreEqual(4, chunks[0].Tokens);
            Assert.AreEqual("one two three four", chunks[0].Text);
            Assert.AreEqual("four five six seven", chunks[1].Text);
            Assert.AreEqual("seven eight nine ten", chunks[2].Text);
        }

        [Test]
        public void ChunkText_ShortText_OneChunk()
        {
            var text = "alpha beta gamma";
            var chunker = new SimpleChunker(chunkSize: 5);
            var chunks = chunker.ChunkText(text, "wiki", "src").ToList();
            Assert.AreEqual(1, chunks.Count);
            Assert.AreEqual("alpha beta gamma", chunks[0].Text);
        }
    }
}

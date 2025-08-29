using System.Collections.Generic;
using McpServer.Core.Models;

namespace McpServer.Processing.Chunking
{
    public interface IChunker
    {
        IEnumerable<ContextChunk> ChunkText(string text, string sourceType, string sourceLocator, string? sectionAnchor = null, string? gameVersion = null, string? modName = null);
    }
}

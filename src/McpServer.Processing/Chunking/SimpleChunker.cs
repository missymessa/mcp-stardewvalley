using System;
using System.Collections.Generic;
using System.Linq;
using McpServer.Core.Models;

namespace McpServer.Processing.Chunking
{
    public class SimpleChunker : IChunker
    {
        private readonly int _chunkSize;
        private readonly int _overlap;

        public SimpleChunker(int chunkSize = 500, int overlap = 50)
        {
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
            if (overlap < 0) throw new ArgumentOutOfRangeException(nameof(overlap));
            _chunkSize = chunkSize;
            _overlap = overlap;
        }

        public IEnumerable<ContextChunk> ChunkText(string text, string sourceType, string sourceLocator, string? sectionAnchor = null, string? gameVersion = null, string? modName = null)
        {
            if (string.IsNullOrEmpty(text)) yield break;

            var words = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            int start = 0;
            while (start < words.Length)
            {
                int length = Math.Min(_chunkSize, words.Length - start);
                var chunkWords = words.Skip(start).Take(length).ToArray();
                var chunkText = string.Join(' ', chunkWords);

                yield return new ContextChunk
                {
                    Id = Guid.NewGuid(),
                    Text = chunkText,
                    Tokens = chunkWords.Length,
                    SourceType = sourceType,
                    SourceLocator = sourceLocator,
                    SectionAnchor = sectionAnchor,
                    GameVersion = gameVersion,
                    ModName = modName
                };

                if (start + length >= words.Length) break;
                start += Math.Max(1, length - _overlap);
            }
        }
    }
}

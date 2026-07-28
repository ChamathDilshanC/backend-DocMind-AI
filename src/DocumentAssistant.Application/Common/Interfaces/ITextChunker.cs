namespace DocumentAssistant.Application.Common.Interfaces;

public record TextChunk(int ChunkIndex, string Text, int PageNumber, int TokenCount);

public interface ITextChunker
{
    /// <summary>Word-based sliding window chunking. Default: 500-word chunks with 100-word overlap, per spec.</summary>
    IReadOnlyList<TextChunk> Chunk(IReadOnlyList<ExtractedPage> pages, int chunkSizeWords = 500, int overlapWords = 100);
}

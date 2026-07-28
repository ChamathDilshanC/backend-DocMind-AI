using DocumentAssistant.Application.Common.Interfaces;
using SharpToken;

namespace DocumentAssistant.Infrastructure.DocumentProcessing;

/// <summary>
/// Word-based sliding window chunking per page, matching the spec's example exactly:
/// chunk 1 = words 1-500, chunk 2 = words 401-900, chunk 3 = words 801-1300 (step = chunkSize - overlap = 400).
/// </summary>
public class SlidingWindowTextChunker : ITextChunker
{
    private static readonly GptEncoding Encoding = GptEncoding.GetEncoding("cl100k_base");

    public IReadOnlyList<TextChunk> Chunk(IReadOnlyList<ExtractedPage> pages, int chunkSizeWords = 500, int overlapWords = 100)
    {
        var step = chunkSizeWords - overlapWords;
        if (step <= 0)
        {
            throw new ArgumentException("Overlap must be smaller than chunk size.");
        }

        var chunks = new List<TextChunk>();
        var chunkIndex = 0;

        foreach (var page in pages)
        {
            var words = page.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) continue;

            for (var start = 0; start < words.Length; start += step)
            {
                var windowWords = words.Skip(start).Take(chunkSizeWords).ToArray();
                var text = string.Join(' ', windowWords);

                chunks.Add(new TextChunk(chunkIndex, text, page.PageNumber, Encoding.Encode(text).Count));
                chunkIndex++;

                if (start + chunkSizeWords >= words.Length) break;
            }
        }

        return chunks;
    }
}

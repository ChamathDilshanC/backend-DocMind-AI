using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Infrastructure.DocumentProcessing;
using FluentAssertions;
using Xunit;

namespace DocumentAssistant.Tests.Infrastructure;

public class SlidingWindowTextChunkerTests
{
    private readonly SlidingWindowTextChunker _sut = new();

    private static string WordsText(int count) => string.Join(' ', Enumerable.Range(1, count).Select(i => $"w{i}"));

    [Fact]
    public void Chunk_MatchesSpecExample_WordRangesFor1300Words()
    {
        var pages = new List<ExtractedPage> { new(1, WordsText(1300)) };

        var chunks = _sut.Chunk(pages, chunkSizeWords: 500, overlapWords: 100);

        chunks.Should().HaveCount(3);
        chunks[0].Text.Split(' ').First().Should().Be("w1");
        chunks[0].Text.Split(' ').Last().Should().Be("w500");

        chunks[1].Text.Split(' ').First().Should().Be("w401");
        chunks[1].Text.Split(' ').Last().Should().Be("w900");

        chunks[2].Text.Split(' ').First().Should().Be("w801");
        chunks[2].Text.Split(' ').Last().Should().Be("w1300");
    }

    [Fact]
    public void Chunk_AssignsSequentialGlobalChunkIndex()
    {
        var pages = new List<ExtractedPage> { new(1, WordsText(600)), new(2, WordsText(600)) };

        var chunks = _sut.Chunk(pages);

        chunks.Select(c => c.ChunkIndex).Should().BeInAscendingOrder();
        chunks.Select(c => c.ChunkIndex).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Chunk_ShorterThanChunkSize_ProducesSingleChunk()
    {
        var pages = new List<ExtractedPage> { new(1, WordsText(50)) };

        var chunks = _sut.Chunk(pages);

        chunks.Should().ContainSingle();
        chunks[0].PageNumber.Should().Be(1);
    }

    [Fact]
    public void Chunk_EmptyPage_ProducesNoChunks()
    {
        var pages = new List<ExtractedPage> { new(1, "") };

        var chunks = _sut.Chunk(pages);

        chunks.Should().BeEmpty();
    }
}

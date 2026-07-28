using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentAssistant.Infrastructure.BackgroundJobs;

public class DocumentProcessingJob(
    IApplicationDbContext context,
    IStorageService storageService,
    IDocumentTextExtractorFactory extractorFactory,
    ITextChunker textChunker,
    IEmbeddingService embeddingService,
    IVectorStoreService vectorStoreService,
    INotificationService notificationService,
    ILogger<DocumentProcessingJob> logger)
    : IDocumentProcessingJob
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await context.Documents.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document is null)
        {
            logger.LogWarning("DocumentProcessingJob: document {DocumentId} not found", documentId);
            return;
        }

        try
        {
            document.Status = DocumentStatus.Processing;
            document.ProcessingError = null;
            await context.SaveChangesAsync(cancellationToken);
            await notificationService.SendDocumentStatusChangedAsync(document.UserId, document.Id, document.Status.ToString(), cancellationToken: cancellationToken);

            // Idempotent: clear any chunks/vectors from a previous attempt before regenerating.
            var existingChunks = await context.Chunks.Where(c => c.DocumentId == documentId).ToListAsync(cancellationToken);
            context.Chunks.RemoveRange(existingChunks);
            await vectorStoreService.DeleteByDocumentIdAsync(documentId, cancellationToken);

            await notificationService.SendDocumentProgressAsync(document.UserId, document.Id, "Extracting text", 10, cancellationToken);
            await using var fileStream = await storageService.OpenReadAsync(document.StoragePath, cancellationToken);
            var extractor = extractorFactory.GetExtractor(document.FileType);
            var pages = await extractor.ExtractAsync(fileStream, cancellationToken);

            await notificationService.SendDocumentProgressAsync(document.UserId, document.Id, "Chunking", 30, cancellationToken);
            var textChunks = textChunker.Chunk(pages);

            if (textChunks.Count == 0)
            {
                throw new InvalidOperationException("No extractable text was found in this document.");
            }

            await notificationService.SendDocumentProgressAsync(document.UserId, document.Id, "Generating embeddings", 50, cancellationToken);
            var embeddings = await embeddingService.GenerateEmbeddingsAsync(textChunks.Select(c => c.Text).ToList(), cancellationToken);

            var now = DateTime.UtcNow;
            var chunkEntities = new List<Domain.Entities.Chunk>();
            var vectorPoints = new List<VectorPoint>();

            for (var i = 0; i < textChunks.Count; i++)
            {
                var chunk = textChunks[i];
                var embeddingId = Guid.NewGuid();

                chunkEntities.Add(new Domain.Entities.Chunk
                {
                    DocumentId = document.Id,
                    ChunkIndex = chunk.ChunkIndex,
                    Text = chunk.Text,
                    EmbeddingId = embeddingId,
                    PageNumber = chunk.PageNumber,
                    TokenCount = chunk.TokenCount
                });

                vectorPoints.Add(new VectorPoint(
                    embeddingId, embeddings[i], document.UserId, document.Id, chunk.PageNumber, chunk.ChunkIndex, document.Name, now));
            }

            await notificationService.SendDocumentProgressAsync(document.UserId, document.Id, "Storing vectors", 80, cancellationToken);
            context.Chunks.AddRange(chunkEntities);
            await vectorStoreService.UpsertBatchAsync(vectorPoints, cancellationToken);

            document.Status = DocumentStatus.Completed;
            document.PageCount = pages.Select(p => p.PageNumber).Distinct().Count();
            await context.SaveChangesAsync(cancellationToken);

            await notificationService.SendDocumentStatusChangedAsync(document.UserId, document.Id, document.Status.ToString(), cancellationToken: cancellationToken);
            await notificationService.SendDocumentProgressAsync(document.UserId, document.Id, "Completed", 100, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DocumentProcessingJob failed for document {DocumentId}", documentId);

            document.Status = DocumentStatus.Failed;
            document.ProcessingError = ex.Message;
            await context.SaveChangesAsync(cancellationToken);
            await notificationService.SendDocumentStatusChangedAsync(document.UserId, document.Id, document.Status.ToString(), ex.Message, cancellationToken);

            throw;
        }
    }
}

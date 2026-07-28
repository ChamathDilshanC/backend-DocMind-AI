using DocumentAssistant.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Document> Documents { get; }
    DbSet<Chunk> Chunks { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

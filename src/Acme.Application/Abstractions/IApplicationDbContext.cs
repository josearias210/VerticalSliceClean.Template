using Acme.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acme.Application.Abstractions
{
    public interface IApplicationDbContext
    {
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<Acme.Domain.Entities.Account> Accounts { get; }
        
        // Example entity - TodoItem demonstrates CRUD patterns
        DbSet<TodoItem> TodoItems { get; }
        
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}

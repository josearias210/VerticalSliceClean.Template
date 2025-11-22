using Acme.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acme.Application.Abstractions
{
    public interface IApplicationDbContext
    {
        DbSet<Account> Accounts { get; }
        
        // Example entity - TodoItem demonstrates CRUD patterns
        DbSet<TodoItem> TodoItems { get; }
        
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}

namespace Acme.Application.Abstractions;

using Acme.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public interface IApplicationDbContext
{
    DbSet<Account> Accounts { get; }
    
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

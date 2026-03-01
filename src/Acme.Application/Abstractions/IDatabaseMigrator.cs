namespace Acme.Application.Abstractions;

public interface IDatabaseMigrator
{
    Task ApplyMigrationsAsync(CancellationToken cancellationToken = default);
}

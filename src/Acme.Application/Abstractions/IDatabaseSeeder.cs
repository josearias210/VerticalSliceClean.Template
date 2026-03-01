namespace Acme.Application.Abstractions;

public interface IDatabaseSeeder
{
    Task SeedRolesAsync(CancellationToken cancellationToken = default);
    Task SeedAdminUserAsync(CancellationToken cancellationToken = default);
    Task SeedOpenIddictClientAsync(CancellationToken cancellationToken = default);
}

namespace Acme.Infrastructure.Settings;

/// <summary>
/// Database initialization settings for development and production environments.
/// Controls automatic migrations and seeding behavior on application startup.
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Applies pending EF Core migrations automatically on startup.
    /// Set to false in production - use manual migrations instead.
    /// </summary>
    public required bool ApplyMigrationsOnStartup { get; set; }

    /// <summary>
    /// Seeds default roles (Admin, User) if they don't exist.
    /// Safe to keep true in all environments.
    /// </summary>
    public required bool SeedRolesOnStartup { get; set; }

    /// <summary>
    /// Creates admin user from AdminUserSettings if not exists.
    /// Set to false in production after initial setup.
    /// </summary>
    public required bool SeedAdminOnStartup { get; set; }
}


namespace Acme.Infrastructure.Persistence.EF.Configurations;

using Acme.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework configuration for Account (extends IdentityUser).
/// Configures custom properties added to the base Identity model.
/// </summary>
public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        // Custom properties configuration
        builder.Property(x => x.FullName)
            .HasMaxLength(200);

        builder.Property(x => x.PreferredUsername)
            .HasMaxLength(50);

        // Index for efficient username lookup
        builder.HasIndex(x => x.PreferredUsername)
            .HasDatabaseName("IX_Accounts_PreferredUsername");
    }
}

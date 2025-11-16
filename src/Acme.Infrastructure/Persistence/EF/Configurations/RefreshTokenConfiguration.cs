namespace Acme.Infrastructure.Persistence.EF.Configurations;

using Acme.Domain.Entities;
using Acme.Infrastructure.Persistence.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", ApplicationDbContext.Schema);

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(88); // Base64 hash length
        
        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450); // Identity UserId length

        // Foreign key to Account with CASCADE delete
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index for token hash (fast lookup)
        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_TokenHash");

        // Composite index for active token queries (most common query pattern)
        // Filters: WHERE UserId = @userId AND RevokedAt IS NULL AND ExpiresAt > GETUTCDATE()
        builder.HasIndex(x => new { x.UserId, x.RevokedAt, x.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_Active")
            .HasFilter("[RevokedAt] IS NULL");
    }
}

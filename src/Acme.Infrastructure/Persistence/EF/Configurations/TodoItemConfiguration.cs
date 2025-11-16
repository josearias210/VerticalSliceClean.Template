using Acme.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acme.Infrastructure.Persistence.EF.Configurations;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("TodoItems");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.CompletedAt)
            .IsRequired(false);

        builder.Property(t => t.CreatedByAccountId)
            .IsRequired();

        // Relationship with Account
        builder.HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedByAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite index for the most common query: filter by user and completion status
        // This covers: GetTodoItems filtered by CreatedByAccountId and optionally IsCompleted
        builder.HasIndex(t => new { t.CreatedByAccountId, t.IsCompleted })
            .HasDatabaseName("IX_TodoItems_CreatedByAccountId_IsCompleted");

        // Index for ordering by creation date (used in GetTodoItems with OrderByDescending)
        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("IX_TodoItems_CreatedAt");
    }
}

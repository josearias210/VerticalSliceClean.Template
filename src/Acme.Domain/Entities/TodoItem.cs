namespace Acme.Domain.Entities;

/// <summary>
/// Example entity demonstrating vertical slice architecture patterns.
/// Represents a simple todo item with ownership and completion tracking.
/// Replace this with your actual domain entities when building your application.
/// </summary>
public sealed class TodoItem
{
    /// <summary>
    /// Unique identifier for the todo item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Title or name of the todo item. Required, max 200 characters.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Optional detailed description of the todo item. Max 1000 characters.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether the todo item has been completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// UTC timestamp when the todo item was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the todo item was marked as completed. Null if not completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// ID of the account that created this todo item.
    /// Used for user-scoped queries and authorization.
    /// </summary>
    public string CreatedByAccountId { get; set; } = string.Empty;
    
    /// <summary>
    /// Navigation property to the account that created this todo item.
    /// </summary>
    public Account? CreatedBy { get; set; }
}

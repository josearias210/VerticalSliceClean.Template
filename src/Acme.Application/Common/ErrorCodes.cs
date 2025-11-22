namespace Acme.Application.Common;

/// <summary>
/// Centralized error codes for consistent error handling across the application.
/// Using constants ensures typo-free error codes and easier refactoring.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// TodoItem entity related error codes (example domain errors).
    /// </summary>
    public static class TodoItem
    {
        public const string NotFound = "TodoItem.NotFound";
        public const string Forbidden = "TodoItem.Forbidden";
        public const string InvalidOperation = "TodoItem.InvalidOperation";
    }

    // Add more domain-specific error code classes as your application grows
    // Example:
    // public static class Product
    // {
    //     public const string NotFound = "Product.NotFound";
    //     public const string OutOfStock = "Product.OutOfStock";
    //     public const string PriceInvalid = "Product.PriceInvalid";
    // }
}

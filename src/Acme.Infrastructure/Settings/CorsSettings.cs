using System.ComponentModel.DataAnnotations;

namespace Acme.Infrastructure.Settings;

/// <summary>
/// CORS (Cross-Origin Resource Sharing) configuration.
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// List of allowed origins for CORS (e.g., http://localhost:3000).
    /// </summary>
    [Required(ErrorMessage = "At least one allowed origin is required")]
    [MinLength(1, ErrorMessage = "At least one allowed origin must be configured")]
    public required string[] AllowedOrigins { get; set; }

    /// <summary>
    /// Whether to allow credentials (cookies, authorization headers) in CORS requests.
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>
    /// Additional allowed headers beyond the default CORS headers.
    /// </summary>
    public string[] AllowedHeaders { get; set; } = ["Content-Type", "Authorization", "X-Requested-With"];

    /// <summary>
    /// Allowed HTTP methods for CORS requests.
    /// </summary>
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH"];

    /// <summary>
    /// Headers exposed to the browser in CORS responses.
    /// </summary>
    public string[] ExposedHeaders { get; set; } = ["Content-Disposition"];

    /// <summary>
    /// Max age in seconds for preflight cache (default: 1 hour).
    /// </summary>
    [Range(0, 86400, ErrorMessage = "MaxAgeSeconds must be between 0 and 86,400 (24 hours)")]
    public int MaxAgeSeconds { get; set; } = 3600;
}

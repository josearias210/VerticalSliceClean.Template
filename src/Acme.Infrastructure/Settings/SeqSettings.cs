using System.ComponentModel.DataAnnotations;

namespace Acme.Infrastructure.Settings;

/// <summary>
/// Seq structured logging server configuration.
/// </summary>
public class SeqSettings
{
    /// <summary>
    /// Seq server URL (e.g., http://localhost:5341).
    /// </summary>
    [Required(ErrorMessage = "Seq ServerUrl is required")]
    [Url(ErrorMessage = "Seq ServerUrl must be a valid URL")]
    public required string ServerUrl { get; set; }

    /// <summary>
    /// Seq API key for authentication (optional for local development).
    /// Store in User Secrets for production.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Path to buffer logs when Seq is unavailable.
    /// </summary>
    [Required(ErrorMessage = "BufferPath is required")]
    public required string BufferPath { get; set; } = "logs/seq-buffer";

    /// <summary>
    /// Maximum number of events to buffer when Seq is down.
    /// </summary>
    [Range(1000, 1000000, ErrorMessage = "QueueSizeLimit must be between 1,000 and 1,000,000")]
    public int QueueSizeLimit { get; set; } = 100000;

    /// <summary>
    /// Interval in seconds to batch and send logs to Seq.
    /// </summary>
    [Range(1, 60, ErrorMessage = "BatchPostingLimit must be between 1 and 60 seconds")]
    public int BatchPeriodSeconds { get; set; } = 2;
}

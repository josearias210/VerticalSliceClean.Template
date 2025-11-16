using System.ComponentModel.DataAnnotations;

namespace Acme.Infrastructure.Settings;

/// <summary>
/// OpenTelemetry distributed tracing and metrics configuration.
/// </summary>
public class OpenTelemetrySettings
{
    /// <summary>
    /// OTLP endpoint for traces and metrics (e.g., http://localhost:4317 for Jaeger).
    /// </summary>
    [Required(ErrorMessage = "OTLP Endpoint is required")]
    [Url(ErrorMessage = "OTLP Endpoint must be a valid URL")]
    public required string OtlpEndpoint { get; set; }

    /// <summary>
    /// Service name for identifying traces and metrics.
    /// </summary>
    [Required(ErrorMessage = "ServiceName is required")]
    [MinLength(1, ErrorMessage = "ServiceName cannot be empty")]
    public required string ServiceName { get; set; }

    /// <summary>
    /// Service version for versioning traces.
    /// </summary>
    [Required(ErrorMessage = "ServiceVersion is required")]
    public required string ServiceVersion { get; set; }

    /// <summary>
    /// Export timeout in milliseconds (default: 3000ms).
    /// </summary>
    [Range(1000, 30000, ErrorMessage = "ExportTimeoutMilliseconds must be between 1,000 and 30,000")]
    public int ExportTimeoutMilliseconds { get; set; } = 3000;

    /// <summary>
    /// Maximum queue size for batch export.
    /// </summary>
    [Range(512, 10000, ErrorMessage = "MaxQueueSize must be between 512 and 10,000")]
    public int MaxQueueSize { get; set; } = 2048;

    /// <summary>
    /// Maximum batch size for export.
    /// </summary>
    [Range(128, 2048, ErrorMessage = "MaxExportBatchSize must be between 128 and 2,048")]
    public int MaxExportBatchSize { get; set; } = 512;
}

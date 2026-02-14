# Infrastructure Reference

## Serilog Logging

**File:** `src/Acme.Infrastructure/Extensions/SerilogExtensions.cs`

Configured in `Program.cs` via `builder.Host.ConfigureSerilog()` + `app.UseSerilogRequestLogging()`.

### Sinks

| Sink | Target | Min Level | Details |
|------|--------|-----------|---------|
| **Console** | stdout (dev only) | All | Template: `[HH:mm:ss LVL] Message` |
| **Seq** | Structured log server | Information | 100 events/batch, 256KB event limit, buffer to disk |
| **File (app)** | `logs/app-.log` | All | Daily rotation, 10MB limit, 30-day retention |
| **File (errors)** | `logs/errors-.log` | Error | Daily rotation, 10MB limit, 90-day retention |

### Enrichers

```csharp
.Enrich.FromLogContext()
.Enrich.WithMachineName()
.Enrich.WithThreadId()
.Enrich.WithProperty("Application", "Acme")
.Enrich.WithProperty("TraceId", () => Activity.Current?.TraceId.ToString() ?? "N/A")
.Enrich.WithProperty("SpanId", () => Activity.Current?.SpanId.ToString() ?? "N/A")
```

### Log Level Overrides

```csharp
.MinimumLevel.Is(Enum.Parse<LogEventLevel>(logLevel))     // from config
.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
```

### SeqSettings (`Logging:Seq` section)

```csharp
public class SeqSettings
{
    [Required] [Url] public required string ServerUrl { get; set; }
    public string? ApiKey { get; set; }
    [Required] public required string BufferPath { get; set; } = "logs/seq-buffer";
    [Range(1000, 1000000)] public int QueueSizeLimit { get; set; } = 100000;
    [Range(1, 60)] public int BatchPeriodSeconds { get; set; } = 2;
}
```

---

## OpenTelemetry

**File:** `src/Acme.Infrastructure/Extensions/OpenTelemetryExtensions.cs`

### Resource Builder

```csharp
ResourceBuilder.CreateDefault()
    .AddService(settings.ServiceName, serviceVersion: settings.ServiceVersion)
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
    });
```

### Tracing Instrumentation

```csharp
.WithTracing(tracing =>
{
    tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("http.request_content_type", request.ContentType);
                activity.SetTag("http.request_content_length", request.ContentLength);
            };
            options.EnrichWithHttpResponse = (activity, response) =>
            {
                activity.SetTag("http.response_content_type", response.ContentType);
            };
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.EnrichWithIDbCommand = (activity, command) =>
            {
                activity.SetTag("db.operation_name", command.CommandText.Split(' ').FirstOrDefault());
            };
        })
        .AddSource(settings.ServiceName)
        .AddOtlpExporter(...);
})
```

### Metrics Instrumentation

```csharp
.WithMetrics(metrics =>
{
    metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(...);
})
```

### Batch Export Config

```csharp
otlpOptions.Endpoint = new Uri(settings.OtlpEndpoint);
otlpOptions.ExportProcessorType = ExportProcessorType.Batch;
otlpOptions.BatchExportProcessorOptions = new()
{
    MaxQueueSize = settings.MaxQueueSize,              // default 2048
    ScheduledDelayMilliseconds = 5000,
    ExporterTimeoutMilliseconds = settings.ExportTimeoutMilliseconds,  // default 3000
    MaxExportBatchSize = settings.MaxExportBatchSize   // default 512
};
```

### OpenTelemetrySettings (`OpenTelemetry` section)

```csharp
public class OpenTelemetrySettings
{
    [Required] [Url] public required string OtlpEndpoint { get; set; }
    [Required] [MinLength(1)] public required string ServiceName { get; set; }
    [Required] public required string ServiceVersion { get; set; }
    [Range(1000, 30000)] public int ExportTimeoutMilliseconds { get; set; } = 3000;
    [Range(512, 10000)] public int MaxQueueSize { get; set; } = 2048;
    [Range(128, 2048)] public int MaxExportBatchSize { get; set; } = 512;
}
```

---

## Rate Limiting

**File:** `src/Acme.Host/Extensions/RateLimitingExtensions.cs`

### Policies

| Policy | Type | Limit | Window | Queue | Use |
|--------|------|-------|--------|-------|-----|
| `"auth"` | Fixed window | 5 requests | 15 min | 0 (reject) | Login, register |
| `"general"` | Fixed window | 100 requests | 1 min | 5 | General endpoints |
| `"per-ip"` | Concurrency | 10 concurrent | — | 2 | Abuse prevention |

### Rejection Handler

```csharp
options.RejectionStatusCode = 429;
options.OnRejected = async (context, token) =>
{
    context.HttpContext.Response.StatusCode = 429;
    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
    {
        await context.HttpContext.Response.WriteAsync(
            $"Too many requests. Please try again after {retryAfter.TotalSeconds} seconds.", token);
    }
    // ...
};
```

### Usage in Endpoints

```csharp
app.MapGroup("api/v1/accounts").RequireRateLimiting("auth");
```

### Test Bypass

```csharp
if (app.Configuration["TestEnvironment"] != "true")
{
    app.UseRateLimiter();
}
```

---

## Security Headers

**File:** `src/Acme.Infrastructure/Extensions/SecurityHeadersExtensions.cs`

Applied via `app.UseDefaultSecurityHeaders(app.Environment)` in middleware pipeline.

| Header | Value | Notes |
|--------|-------|-------|
| `X-Content-Type-Options` | `nosniff` | Prevent MIME sniffing |
| `X-Frame-Options` | `DENY` | Prevent clickjacking |
| `Referrer-Policy` | `no-referrer` | Privacy |
| `X-XSS-Protection` | `0` | Disable legacy XSS filter (use CSP) |
| `Content-Security-Policy` | `default-src 'self'; img-src 'self' data:; ...` | Restrict resource loading |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains; preload` | **Production only** |

CSP is **skipped** for `/scalar` and `/openapi` paths (API documentation needs inline scripts).

---

## CORS

**File:** `src/Acme.Infrastructure/Extensions/CorsExtensions.cs`

### Configuration Pattern

```csharp
// Dynamic from CorsSettings
bool allowAnyOrigin = corsSettings.AllowedOrigins.Contains("*");

if (allowAnyOrigin)
{
    policy.AllowAnyOrigin()...;
    // CRITICAL: Cannot use AllowCredentials() with AllowAnyOrigin()
}
else if (corsSettings.AllowedOrigins.Length > 0)
{
    policy.WithOrigins(corsSettings.AllowedOrigins)...;
    if (corsSettings.AllowCredentials) policy.AllowCredentials();
}
else
{
    policy.WithOrigins(); // Deny all (safe default)
}
```

### CorsSettings (`Cors` section)

```csharp
public class CorsSettings
{
    [Required] [MinLength(1)] public required string[] AllowedOrigins { get; set; }
    public bool AllowCredentials { get; set; } = true;
    public string[] AllowedHeaders { get; set; } = ["Content-Type", "Authorization", "X-Requested-With"];
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH"];
    public string[] ExposedHeaders { get; set; } = ["Content-Disposition"];
    [Range(0, 86400)] public int MaxAgeSeconds { get; set; } = 3600;
}
```

---

## JSON Serialization

**File:** `src/Acme.Api/DependencyInjection.cs`

```csharp
services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

- Null values omitted from responses
- Enums serialized as strings (not numbers)

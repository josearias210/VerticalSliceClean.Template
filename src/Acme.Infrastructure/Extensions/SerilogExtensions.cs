using Acme.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using System.Globalization;

namespace Acme.Infrastructure.Extensions;

public static class SerilogExtensions
{
    public static IHostBuilder ConfigureSerilog(this IHostBuilder host)
    {
        return host.UseSerilog((context, services, configuration) =>
        {
            // Get SeqSettings from IOptions (with validation)
            var seqSettings = services.GetRequiredService<IOptions<SeqSettings>>().Value;
            var logLevel = context.Configuration["Logging:MinimumLevel"] ?? "Information";

            configuration
                .MinimumLevel.Is(Enum.Parse<LogEventLevel>(logLevel))
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "Acme")
                // Enrich with OpenTelemetry TraceId/SpanId for correlation with Jaeger
                .Enrich.WithProperty("TraceId", () => System.Diagnostics.Activity.Current?.TraceId.ToString() ?? "N/A")
                .Enrich.WithProperty("SpanId", () => System.Diagnostics.Activity.Current?.SpanId.ToString() ?? "N/A");

            // Console output (development)
            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture);
            }

            // Seq (structured logging server) - with resilience
            configuration.WriteTo.Seq(
                serverUrl: seqSettings.ServerUrl,
                apiKey: seqSettings.ApiKey,
                restrictedToMinimumLevel: LogEventLevel.Information,
                bufferBaseFilename: seqSettings.BufferPath,
                eventBodyLimitBytes: 262_144,            // 256KB per event
                batchPostingLimit: 100,                  // Max 100 events per batch
                period: TimeSpan.FromSeconds(seqSettings.BatchPeriodSeconds),
                queueSizeLimit: seqSettings.QueueSizeLimit,
                formatProvider: CultureInfo.InvariantCulture); // <-- Solución CA1305

            // File logging (persistent)
            configuration.WriteTo.File(
                path: "logs/app-.log",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 10_485_760, // 10 MB
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture);

            // Error-only file (long retention)
            configuration.WriteTo.File(
                path: "logs/errors-.log",
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 10_485_760,
                retainedFileCountLimit: 90,
                formatProvider: CultureInfo.InvariantCulture);
        });
    }
}

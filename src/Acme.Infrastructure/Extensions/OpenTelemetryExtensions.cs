using Acme.Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Acme.Infrastructure.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryInstrumentation(
        this IServiceCollection services,
        OpenTelemetrySettings settings)
    {
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(settings.ServiceName, serviceVersion: settings.ServiceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
            });

        // Add OpenTelemetry Tracing
        services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(settings.ServiceName, serviceVersion: settings.ServiceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
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
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(settings.OtlpEndpoint);
                        otlpOptions.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
                        otlpOptions.BatchExportProcessorOptions = new()
                        {
                            MaxQueueSize = settings.MaxQueueSize,
                            ScheduledDelayMilliseconds = 5000,
                            ExporterTimeoutMilliseconds = settings.ExportTimeoutMilliseconds,
                            MaxExportBatchSize = settings.MaxExportBatchSize
                        };
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(settings.OtlpEndpoint);
                        otlpOptions.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
                        otlpOptions.BatchExportProcessorOptions = new()
                        {
                            MaxQueueSize = settings.MaxQueueSize,
                            ScheduledDelayMilliseconds = 5000,
                            ExporterTimeoutMilliseconds = settings.ExportTimeoutMilliseconds,
                            MaxExportBatchSize = settings.MaxExportBatchSize
                        };
                    });
            });

        return services;
    }
}

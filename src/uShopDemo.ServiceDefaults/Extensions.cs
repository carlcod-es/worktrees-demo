using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Microsoft.Extensions.Hosting;

// ADR-0034: logs, traces and metrics for the Aspire dashboard. Exports over OTLP only when Aspire (or the
// environment) supplies OTEL_EXPORTER_OTLP_ENDPOINT, so tests and runs outside Aspire send nothing.
public static class Extensions
{
    private const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var otel = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(o =>
                    o.Filter = context => !TelemetryPaths.IsExcluded(context.Request.Path))
                .AddHttpClientInstrumentation()
                // Umbraco background jobs issue many parentless database spans that bury page traces;
                // keep only spans under a recorded parent (Activity.Current is the SQL span itself).
                .AddSqlClientInstrumentation(o =>
                    o.Filter = _ => Activity.Current?.Parent is { Recorded: true }));

        if (!string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointKey]))
            otel.UseOtlpExporter();

        return builder;
    }

    /// <summary>
    /// Site only. Umbraco owns logging through Serilog and its pipeline reads <see cref="ILogEventSink"/> services
    /// (ReadFrom.Services), so register the OTLP sink as one. Endpoint, protocol, headers, service name and
    /// resource attributes come from the OTEL_* environment variables Aspire sets (the sink reads them itself).
    /// </summary>
    public static TBuilder AddSerilogOtlpSink<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        if (string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointKey]))
            return builder;

        // A Logger is an ILogEventSink; the container disposes it at shutdown, which flushes pending batches.
        builder.Services.AddSingleton<ILogEventSink>(_ => new LoggerConfiguration()
            // The exporters' own HttpClient logs would feed back into the sink on every export.
            .Filter.ByExcluding(e => e.Properties.TryGetValue("SourceContext", out var source)
                && source is ScalarValue { Value: string category }
                && category.StartsWith("System.Net.Http.HttpClient.Otlp", StringComparison.Ordinal))
            .WriteTo.OpenTelemetry(options =>
                // The sink's own export calls must not produce spans.
                options.OnBeginSuppressInstrumentation = SuppressInstrumentationScope.Begin)
            .CreateLogger());

        return builder;
    }
}

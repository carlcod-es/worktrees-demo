using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Hosting;

/// <summary>ADR-0034: requests not worth a span (health checks, static assets, backoffice client files).</summary>
internal static class TelemetryPaths
{
    private static readonly string[] ExcludedPrefixes =
        ["/health", "/alive", "/umbraco/backoffice", "/css", "/js", "/media", "/img", "/favicon.ico"];

    public static bool IsExcluded(PathString path) =>
        ExcludedPrefixes.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
}

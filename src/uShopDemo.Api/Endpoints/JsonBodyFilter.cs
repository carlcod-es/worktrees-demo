using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Net.Http.Headers;

namespace uShopDemo.Api.Endpoints;

/// <summary>CSRF defence: rejects non-application/json content types, notably application/*+json variants, with 415 after the session check. Routing is told not to filter (see WithAnyContentType); minimal-API body binding already returns 415 for most other non-JSON types. Cross-origin, application/json forces a CORS preflight.</summary>
public sealed class JsonBodyFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var contentType = context.HttpContext.Request.ContentType;
        if (!MediaTypeHeaderValue.TryParse(contentType, out var media)
            || !media.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            return ValueTask.FromResult<object?>(Results.StatusCode(StatusCodes.Status415UnsupportedMediaType));

        return next(context);
    }
}

public static class AnyContentTypeExtensions
{
    /// <summary>
    /// Routing must not filter these endpoints by content type. Minimal APIs infer an <see cref="IAcceptsMetadata"/>
    /// (application/json) from the body parameter, and routing's ConsumesMatcherPolicy then replaces the endpoint with a
    /// 415 endpoint during selection, so <c>SessionMiddleware</c> never sees <c>RequiresSessionMetadata</c> and an
    /// unauthenticated text/plain request gets 415 instead of 401. An empty content-type list means "any" to routing;
    /// the 415 is produced after the session check by <see cref="JsonBodyFilter"/> (and body binding).
    /// Must be applied per endpoint, after mapping: group metadata would be overridden by the inferred metadata.
    /// </summary>
    public static TBuilder WithAnyContentType<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new AnyContentType());

    private sealed class AnyContentType : IAcceptsMetadata
    {
        public IReadOnlyList<string> ContentTypes { get; } = [];
        public Type? RequestType => null;
        public bool IsOptional => false;
    }
}

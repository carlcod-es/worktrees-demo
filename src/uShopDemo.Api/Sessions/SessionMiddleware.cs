namespace uShopDemo.Api.Sessions;

/// <summary>Endpoint metadata marking an endpoint as requiring a valid session.</summary>
public sealed class RequiresSessionMetadata
{
    public static RequiresSessionMetadata Instance { get; } = new();
}

/// <summary>
/// Resolves the session cookie into <see cref="SessionContext"/> for endpoints marked with
/// <see cref="RequiresSessionMetadata"/>, or short-circuits with an empty 401. Runs before model binding,
/// so an unauthenticated request gets 401 regardless of its body or content type.
/// </summary>
public sealed class SessionMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext http, SessionCookie cookie, SessionContext session)
    {
        if (http.GetEndpoint()?.Metadata.GetMetadata<RequiresSessionMetadata>() is null
            || HttpMethods.IsOptions(http.Request.Method))
            return next(http);

        if (cookie.Read(http.Request) is not { } id)
        {
            http.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        session.Set(id);
        return next(http);
    }
}

using uShopDemo.Api.Sessions;

namespace uShopDemo.Api.Endpoints;

public static class SessionEndpoints
{
    // Deliberately outside the session group: no session required, no JSON body filter (no body is read).
    public static void MapSessionEndpoints(this WebApplication app)
    {
        // Read once, like the CORS policy, so the two cannot diverge.
        var webOrigin = WebOrigin.Read(app.Configuration);
        app.MapPost("/session", (HttpContext http, SessionCookie cookie) =>
        {
            // A cross-site form POST must not reset the visitor's session (SameSite=Strict withholds the cookie,
            // so a fresh id would be issued). Requests with no Origin (curl, same-origin tools) proceed.
            // The API's own origin is allowed too: the Scalar UI is served from it (same-origin, so CSRF-neutral).
            var ownOrigin = $"{http.Request.Scheme}://{http.Request.Host}";
            if (http.Request.Headers.TryGetValue("Origin", out var origin)
                && !WebOrigin.Matches(origin.ToString(), webOrigin)
                && !WebOrigin.Matches(origin.ToString(), ownOrigin))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            // Valid cookie: re-issue the same id with a fresh lifetime. Otherwise a new id.
            cookie.Issue(http, cookie.Read(http.Request) ?? SessionCookie.NewSessionId());
            return Results.NoContent();
        })
        .WithTags("Session").WithName("StartSession").WithSummary("Start or refresh the session (sets the session cookie)")
        .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status403Forbidden);
    }
}

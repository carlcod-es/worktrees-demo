namespace uShopDemo.Api.Sessions;

/// <summary>The caller's session for the current request. Populated by <see cref="SessionMiddleware"/>; never logged or returned.</summary>
public sealed class SessionContext
{
    private Guid? id;

    public Guid Id => id ?? throw new InvalidOperationException("No session on this request.");

    internal void Set(Guid sessionId) => id = sessionId;
}

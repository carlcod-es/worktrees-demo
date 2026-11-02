using Microsoft.Extensions.Caching.Memory;

namespace uShopDemo.Api.Carts;

public static class OperationStatus
{
    public const string Queued = "queued";
    public const string Completed = "completed";
    public const string Failed = "failed";
}

/// <summary>Snapshot of an operation. <paramref name="SessionId"/> is the owner; callers must enforce ownership.</summary>
public sealed record OperationInfo(Guid OperationId, string Status, string? Error, string? OrderNumber, Guid SessionId);

/// <summary>Operation state held in <see cref="IMemoryCache"/>; entries expire 10 minutes after their last update.</summary>
public sealed class OperationTracker(IMemoryCache cache)
{
    public static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);

    private static string OpKey(Guid id) => $"cart-op:{id}";
    private static string OrderKey(string orderNumber) => $"cart-order:{orderNumber}";

    public OperationInfo? Get(Guid operationId) =>
        cache.TryGetValue(OpKey(operationId), out OperationInfo? info) ? info : null;

    /// <summary>The checkout operation for an order number, or null if unknown or expired.</summary>
    public OperationInfo? GetByOrderNumber(string orderNumber) =>
        cache.TryGetValue(OrderKey(orderNumber), out Guid id) ? Get(id) : null;

    public void MarkQueued(Guid operationId, Guid sessionId, string? orderNumber = null) =>
        Store(new OperationInfo(operationId, OperationStatus.Queued, null, orderNumber, sessionId));

    public void MarkCompleted(Guid operationId) => Update(operationId, OperationStatus.Completed, null);

    public void MarkFailed(Guid operationId, string error) => Update(operationId, OperationStatus.Failed, error);

    private void Update(Guid operationId, string status, string? error)
    {
        if (Get(operationId) is { } existing)
            Store(existing with { Status = status, Error = error });
    }

    private void Store(OperationInfo info)
    {
        cache.Set(OpKey(info.OperationId), info, Expiry);
        if (info.OrderNumber is not null)
        {
            // Keep the first operation for an order number; only refresh expiry for the same operation.
            var key = OrderKey(info.OrderNumber);
            if (!cache.TryGetValue(key, out Guid owner) || owner == info.OperationId)
                cache.Set(key, info.OperationId, Expiry);
        }
    }
}

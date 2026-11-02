namespace uShopDemo.Api.Endpoints;

// Request bodies. Fields are nullable so a missing field is a validation error, not a binding failure.
public sealed record AddLineRequest(Guid? ProductKey, string? Name, string? Sku, decimal? UnitPrice, int? Quantity);

public sealed record UpdateLineRequest(int? Quantity);

public sealed record CheckoutRequest(string? Name, string? Email, string? Address, string? CardNumber)
{
    // Keep the card number out of the generated ToString (and so out of logs).
    private bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append($"Name = {Name}, Email = {Email}, Address = {Address}, CardNumber = ***");
        return true;
    }
}

// Responses. Null members are omitted from the JSON.
public sealed record CartLineResponse(Guid ProductKey, string Name, string Sku, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record CartResponse(Guid CartId, IReadOnlyList<CartLineResponse> Lines, int ItemCount, decimal Total);

public sealed record OperationAccepted(Guid OperationId);

public sealed record CheckoutAccepted(Guid OperationId, string OrderNumber);

public sealed record OperationResponse(Guid OperationId, string Status, string? Error);

/// <summary>Status is pending, confirmed or rejected; only the members for that status are set.</summary>
public sealed record OrderResponse(
    string OrderNumber,
    string Status,
    string? Reason = null,
    string? Name = null,
    string? Email = null,
    string? Address = null,
    DateTime? OrderedUtc = null,
    IReadOnlyList<CartLineResponse>? Lines = null,
    int? ItemCount = null,
    decimal? Total = null);

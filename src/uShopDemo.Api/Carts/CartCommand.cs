namespace uShopDemo.Api.Carts;

/// <summary>A queued cart write. No card data is carried anywhere in the API.</summary>
public abstract record CartCommand(Guid OperationId, Guid CartId, Guid SessionId);

public sealed record AddLine(
    Guid OperationId, Guid CartId, Guid SessionId,
    Guid ProductKey, string Name, string Sku, decimal UnitPrice, int Quantity)
    : CartCommand(OperationId, CartId, SessionId);

/// <summary>Quantity 0 removes the line.</summary>
public sealed record SetQuantity(
    Guid OperationId, Guid CartId, Guid SessionId,
    Guid ProductKey, int Quantity)
    : CartCommand(OperationId, CartId, SessionId);

public sealed record RemoveLine(
    Guid OperationId, Guid CartId, Guid SessionId,
    Guid ProductKey)
    : CartCommand(OperationId, CartId, SessionId);

public sealed record Checkout(
    Guid OperationId, Guid CartId, Guid SessionId,
    string OrderNumber, string Name, string Email, string Address)
    : CartCommand(OperationId, CartId, SessionId);

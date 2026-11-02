namespace uShopDemo.Api.Data;

public enum CartStatus
{
    Open = 0,
    Ordered = 1,
}

/// <summary>A visitor's cart. Becomes an order when <see cref="Status"/> is Ordered. Owned by the session identified by <see cref="SessionId"/>.</summary>
public class Cart
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public CartStatus Status { get; set; }
    public string? OrderNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime? OrderedUtc { get; set; }
    public decimal? Total { get; set; }
    public List<CartLine> Lines { get; set; } = [];
}

/// <summary>Name, SKU and unit price are captured when the line is added.</summary>
public class CartLine
{
    public int Id { get; set; }
    public Guid CartId { get; set; }
    public Guid ProductKey { get; set; }
    public required string ProductName { get; set; }
    public required string Sku { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

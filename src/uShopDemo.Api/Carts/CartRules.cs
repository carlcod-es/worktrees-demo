namespace uShopDemo.Api.Carts;

/// <summary>Limits shared by request validation and the cart worker.</summary>
public static class CartRules
{
    public const int MaxQuantity = 99;

    // 1,000,000.00 x 99 per line keeps line and order totals well inside decimal(18,2).
    public const decimal MaxUnitPrice = 1_000_000m;
}

using System.Globalization;
using System.Net.Mail;
using uShopDemo.Api.Carts;

namespace uShopDemo.Api.Endpoints;

internal static class Validation
{
    private const decimal MaxPrice = CartRules.MaxUnitPrice;

    public static Dictionary<string, string[]> Add(AddLineRequest r)
    {
        var e = new Dictionary<string, string[]>();
        if (r.ProductKey is null || r.ProductKey == Guid.Empty)
            e["productKey"] = ["A product key is required."];
        Text(e, "name", r.Name, 200);
        Text(e, "sku", r.Sku, 64);
        if (r.UnitPrice is not { } price)
            e["unitPrice"] = ["A unit price is required."];
        else if (price < 0 || price > MaxPrice || decimal.Round(price, 2) != price)
            e["unitPrice"] = [string.Create(CultureInfo.InvariantCulture, $"Unit price must be between 0 and {CartRules.MaxUnitPrice:N0}, with at most 2 decimal places.")];
        if (r.Quantity is not { } q || q < 1 || q > CartRules.MaxQuantity)
            e["quantity"] = [$"Quantity must be between 1 and {CartRules.MaxQuantity}."];
        return e;
    }

    public static Dictionary<string, string[]> Update(UpdateLineRequest r)
    {
        var e = new Dictionary<string, string[]>();
        if (r.Quantity is not { } q || q < 0 || q > CartRules.MaxQuantity)
            e["quantity"] = [$"Quantity must be between 0 and {CartRules.MaxQuantity}."];
        return e;
    }

    public static Dictionary<string, string[]> Checkout(CheckoutRequest r)
    {
        var e = new Dictionary<string, string[]>();
        Text(e, "name", r.Name, 200);
        if (Text(e, "email", r.Email, 320) && !IsEmail(r.Email!))
            e["email"] = ["Enter a valid email address."];
        Text(e, "address", r.Address, 1000);
        if (string.IsNullOrWhiteSpace(r.CardNumber))
            e["cardNumber"] = ["A card number is required."];
        return e;
    }

    private static bool Text(Dictionary<string, string[]> errors, string key, string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[key] = ["This field is required."];
            return false;
        }
        if (value.Length > max)
        {
            errors[key] = [$"Must be at most {max} characters."];
            return false;
        }
        return true;
    }

    private static bool IsEmail(string value) =>
        MailAddress.TryCreate(value, out var address) && address.Address == value;
}

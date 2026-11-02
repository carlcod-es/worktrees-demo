using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using uShopDemo.Api.Data;
using static uShopDemo.Api.Tests.ApiHelpers;

namespace uShopDemo.Api.Tests;

public sealed class CheckoutTests : IDisposable
{
    private readonly ApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task Checkout_returns_order_number_and_order_is_confirmed_with_lines_and_total()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget, 2, price: 4.50m);
        await client.AddCompletedAsync(cartId, Gadget, 1, name: "Gadget", sku: "G-1", price: 1.00m);

        var response = await client.CheckoutAsync(cartId);
        var accepted = await ReadAcceptedAsync(response);

        Assert.False(string.IsNullOrEmpty(accepted.OrderNumber));
        Assert.Equal($"/operations/{accepted.OperationId}", response.Headers.Location?.OriginalString);
        Assert.Contains((await client.GetOrderAsync(accepted.OrderNumber)).Status, new[] { "pending", "confirmed" });

        Assert.Equal("completed", (await ApiFactory.WaitForOperationAsync(client, accepted.OperationId)).Status);

        var order = await client.GetOrderAsync(accepted.OrderNumber);
        Assert.Equal("confirmed", order.Status);
        Assert.Equal(3, order.ItemCount);
        Assert.Equal(10.00m, order.Total);
        Assert.NotNull(order.Lines);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(2, order.Lines.Single(l => l.ProductKey == Widget).Quantity);

        var cart = await client.GetCartAsync(cartId);
        Assert.Empty(cart.Lines);
        Assert.Equal(0, cart.ItemCount);
        Assert.Equal(0m, cart.Total);
    }

    [Fact]
    public async Task Confirmed_order_has_customer_details_utc_time_and_no_card_number()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget);
        var accepted = await ReadAcceptedAsync(await client.CheckoutAsync(cartId));
        await ApiFactory.WaitForOperationAsync(client, accepted.OperationId);

        var response = await client.GetAsync($"/orders/{accepted.OrderNumber}");
        var raw = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(raw);
        var root = json.RootElement;

        Assert.Equal("confirmed", root.GetProperty("status").GetString());
        Assert.EndsWith("Z", root.GetProperty("orderedUtc").GetString());
        Assert.Equal("Ada Lovelace", root.GetProperty("name").GetString());
        Assert.Equal("ada@example.test", root.GetProperty("email").GetString());
        Assert.Equal("1 Analytical Way", root.GetProperty("address").GetString());
        Assert.Equal(1, root.GetProperty("lines").GetArrayLength());
        Assert.Equal(1, root.GetProperty("itemCount").GetInt32());
        Assert.Equal(9.99m, root.GetProperty("total").GetDecimal());
        Assert.DoesNotContain(CardNumber, raw);
        Assert.DoesNotContain("cardNumber", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Stored_order_is_one_ordered_cart_with_the_total_and_no_card_number()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget, 2, price: 4.50m);
        await client.AddCompletedAsync(cartId, Gadget, 1, name: "Gadget", sku: "G-1", price: 1.00m);
        var accepted = await ReadAcceptedAsync(await client.CheckoutAsync(cartId));
        Assert.Equal("completed", (await ApiFactory.WaitForOperationAsync(client, accepted.OperationId)).Status);

        var carts = await factory.WithDbAsync(db => db.Carts.Where(c => c.OrderNumber == accepted.OrderNumber).ToListAsync());

        var cart = Assert.Single(carts);
        Assert.Equal(CartStatus.Ordered, cart.Status);
        Assert.Equal(10.00m, cart.Total);
        var strings = typeof(Cart).GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => (string?)p.GetValue(cart));
        Assert.All(strings, v => Assert.DoesNotContain(CardNumber, v ?? ""));
    }

    [Fact]
    public async Task Checkout_on_an_empty_cart_is_rejected_with_reason()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();

        var accepted = await ReadAcceptedAsync(await client.CheckoutAsync(cartId));
        var op = await ApiFactory.WaitForOperationAsync(client, accepted.OperationId);

        Assert.Equal("failed", op.Status);
        var order = await client.GetOrderAsync(accepted.OrderNumber!);
        Assert.Equal("rejected", order.Status);
        Assert.False(string.IsNullOrEmpty(order.Reason));
    }

    [Fact]
    public async Task Unknown_order_is_404_with_empty_body()
    {
        var client = await factory.CreateSessionBrowserAsync();

        var response = await client.GetAsync("/orders/US-000000-00000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertEmptyBodyAsync(response);
    }
}

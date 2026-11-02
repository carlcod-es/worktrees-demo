using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using static uShopDemo.Api.Tests.ApiHelpers;

namespace uShopDemo.Api.Tests;

public sealed class CartWriteTests : IDisposable
{
    private readonly ApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task Accepted_write_has_location_header_matching_body_operation_id()
    {
        var client = await factory.CreateSessionBrowserAsync();

        var response = await client.PostLineAsync(Guid.NewGuid(), LineBody(Widget));

        var accepted = await ReadAcceptedAsync(response);
        Assert.Equal($"/operations/{accepted.OperationId}", response.Headers.Location?.OriginalString);
        Assert.Equal("completed", (await ApiFactory.WaitForOperationAsync(client, accepted.OperationId)).Status);
    }

    [Fact]
    public async Task First_poll_after_202_is_queued_or_completed()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var accepted = await ReadAcceptedAsync(await client.PostLineAsync(Guid.NewGuid(), LineBody(Widget)));

        var poll = await client.GetAsync($"/operations/{accepted.OperationId}");

        Assert.Equal(HttpStatusCode.OK, poll.StatusCode);
        var body = await poll.Content.ReadFromJsonAsync<OperationResult>();
        Assert.Contains(body!.Status, new[] { "queued", "completed" });
    }

    [Fact]
    public async Task Update_and_remove_responses_carry_location_header()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget);

        var put = await client.PutAsJsonAsync($"/carts/{cartId}/lines/{Widget}", new { quantity = 2 });
        var putAccepted = await ReadAcceptedAsync(put);
        Assert.Equal($"/operations/{putAccepted.OperationId}", put.Headers.Location?.OriginalString);

        var delete = await client.DeleteAsync($"/carts/{cartId}/lines/{Widget}");
        var deleteAccepted = await ReadAcceptedAsync(delete);
        Assert.Equal($"/operations/{deleteAccepted.OperationId}", delete.Headers.Location?.OriginalString);
        await ApiFactory.WaitForOperationAsync(client, deleteAccepted.OperationId);
    }

    [Fact]
    public async Task Add_then_update_applies_in_order()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();

        var add = await ReadAcceptedAsync(await client.PostLineAsync(cartId, LineBody(Widget, 1)));
        var update = await ReadAcceptedAsync(await client.PutAsJsonAsync($"/carts/{cartId}/lines/{Widget}", new { quantity = 5 }));
        Assert.Equal("completed", (await ApiFactory.WaitForOperationAsync(client, add.OperationId)).Status);
        Assert.Equal("completed", (await ApiFactory.WaitForOperationAsync(client, update.OperationId)).Status);

        var line = Assert.Single((await client.GetCartAsync(cartId)).Lines);
        Assert.Equal(Widget, line.ProductKey);
        Assert.Equal(5, line.Quantity);
    }

    [Fact]
    public async Task Add_twice_merges_into_one_line()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();

        await client.AddCompletedAsync(cartId, Widget, 2);
        await client.AddCompletedAsync(cartId, Widget, 3);

        var line = Assert.Single((await client.GetCartAsync(cartId)).Lines);
        Assert.Equal(5, line.Quantity);
    }

    [Fact]
    public async Task Add_twice_caps_quantity_at_99()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();

        await client.AddCompletedAsync(cartId, Widget, 60);
        await client.AddCompletedAsync(cartId, Widget, 60);

        Assert.Equal(99, Assert.Single((await client.GetCartAsync(cartId)).Lines).Quantity);
    }

    [Fact]
    public async Task Update_to_zero_removes_the_line()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget, 2);
        await client.AddCompletedAsync(cartId, Gadget, 1);

        var accepted = await ReadAcceptedAsync(await client.PutAsJsonAsync($"/carts/{cartId}/lines/{Widget}", new { quantity = 0 }));
        Assert.Equal("completed", (await ApiFactory.WaitForOperationAsync(client, accepted.OperationId)).Status);

        var line = Assert.Single((await client.GetCartAsync(cartId)).Lines);
        Assert.Equal(Gadget, line.ProductKey);
    }

    [Fact]
    public async Task Delete_removes_the_line()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget, 2);
        await client.AddCompletedAsync(cartId, Gadget, 1);

        var accepted = await ReadAcceptedAsync(await client.DeleteAsync($"/carts/{cartId}/lines/{Widget}"));
        Assert.Equal("completed", (await ApiFactory.WaitForOperationAsync(client, accepted.OperationId)).Status);

        Assert.Equal(Gadget, Assert.Single((await client.GetCartAsync(cartId)).Lines).ProductKey);
    }

    [Fact]
    public async Task Delete_of_a_line_not_in_the_cart_completes()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget);

        var accepted = await ReadAcceptedAsync(await client.DeleteAsync($"/carts/{cartId}/lines/{Gadget}"));

        Assert.Equal("completed", (await ApiFactory.WaitForOperationAsync(client, accepted.OperationId)).Status);
        Assert.Single((await client.GetCartAsync(cartId)).Lines);
    }

    [Fact]
    public async Task Put_of_a_line_not_in_the_cart_fails_with_reason()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget);

        var accepted = await ReadAcceptedAsync(await client.PutAsJsonAsync($"/carts/{cartId}/lines/{Gadget}", new { quantity = 3 }));
        var op = await ApiFactory.WaitForOperationAsync(client, accepted.OperationId);

        Assert.Equal("failed", op.Status);
        Assert.Equal("line not in cart", op.Error);
    }

    [Fact]
    public async Task Cart_read_returns_line_totals_item_count_and_total()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget, 3, price: 2.50m);
        await client.AddCompletedAsync(cartId, Gadget, 2, name: "Gadget", sku: "G-1", price: 10.00m);

        var cart = await client.GetCartAsync(cartId);

        Assert.Equal(cartId, cart.CartId);
        Assert.Equal(5, cart.ItemCount);
        Assert.Equal(27.50m, cart.Total);
        Assert.Equal(7.50m, cart.Lines.Single(l => l.ProductKey == Widget).LineTotal);
        Assert.Equal(20.00m, cart.Lines.Single(l => l.ProductKey == Gadget).LineTotal);
    }

    [Fact]
    public async Task Name_sku_and_price_are_kept_from_the_first_add()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();

        await client.AddCompletedAsync(cartId, Widget, 1, "First", "SKU-1", 1.00m);
        await client.AddCompletedAsync(cartId, Widget, 1, "Second", "SKU-2", 5.00m);

        var line = Assert.Single((await client.GetCartAsync(cartId)).Lines);
        Assert.Equal("First", line.Name);
        Assert.Equal("SKU-1", line.Sku);
        Assert.Equal(1.00m, line.UnitPrice);
        Assert.Equal(2, line.Quantity);
    }

    [Fact]
    public async Task Caller_supplied_price_is_stored_as_supplied()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();

        await client.AddCompletedAsync(cartId, Widget, 1, price: 1.00m);

        Assert.Equal(1.00m, Assert.Single((await client.GetCartAsync(cartId)).Lines).UnitPrice);
        var stored = await factory.WithDbAsync(db => db.CartLines.AsNoTracking().SingleAsync(l => l.CartId == cartId));
        Assert.Equal(1.00m, stored.UnitPrice);
    }

    [Fact]
    public async Task Write_to_an_ordered_cart_fails()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget);
        var checkout = await ReadAcceptedAsync(await client.CheckoutAsync(cartId));
        Assert.Equal("completed", (await ApiFactory.WaitForOperationAsync(client, checkout.OperationId)).Status);

        var op = await client.AddAndWaitAsync(cartId, Gadget);

        Assert.Equal("failed", op.Status);
        Assert.False(string.IsNullOrEmpty(op.Error));
        Assert.Empty((await client.GetCartAsync(cartId)).Lines);
    }

    [Fact]
    public async Task Unknown_operation_is_404_with_empty_body()
    {
        var client = await factory.CreateSessionBrowserAsync();

        var response = await client.GetAsync($"/operations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertEmptyBodyAsync(response);
    }
}

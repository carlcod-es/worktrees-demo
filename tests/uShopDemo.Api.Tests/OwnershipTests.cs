using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using static uShopDemo.Api.Tests.ApiHelpers;

namespace uShopDemo.Api.Tests;

public sealed class OwnershipTests : IDisposable
{
    private readonly ApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task Second_session_reads_the_cart_as_empty()
    {
        var first = await factory.CreateSessionBrowserAsync();
        var second = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await first.AddCompletedAsync(cartId, Widget, 2);

        var cart = await second.GetCartAsync(cartId);

        Assert.Empty(cart.Lines);
        Assert.Equal(0, cart.ItemCount);
        Assert.Equal(0m, cart.Total);
    }

    [Fact]
    public async Task Second_session_gets_404_writing_to_the_cart_and_original_is_unchanged()
    {
        var first = await factory.CreateSessionBrowserAsync();
        var second = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await first.AddCompletedAsync(cartId, Widget, 2);

        var add = await second.PostLineAsync(cartId, LineBody(Gadget));
        var update = await second.PutAsJsonAsync($"/carts/{cartId}/lines/{Widget}", new { quantity = 9 });
        var delete = await second.DeleteAsync($"/carts/{cartId}/lines/{Widget}");
        var checkout = await second.CheckoutAsync(cartId);

        foreach (var response in new[] { add, update, delete, checkout })
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            await AssertEmptyBodyAsync(response);
        }
        await first.DrainQueueAsync();
        var line = Assert.Single((await first.GetCartAsync(cartId)).Lines);
        Assert.Equal(2, line.Quantity);
    }

    [Fact]
    public async Task Second_session_gets_404_for_the_order_number_and_operation_id()
    {
        var first = await factory.CreateSessionBrowserAsync();
        var second = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await first.AddCompletedAsync(cartId, Widget);
        var accepted = await ReadAcceptedAsync(await first.CheckoutAsync(cartId));
        await ApiFactory.WaitForOperationAsync(first, accepted.OperationId);

        var order = await second.GetAsync($"/orders/{accepted.OrderNumber}");
        var operation = await second.GetAsync($"/operations/{accepted.OperationId}");

        Assert.Equal(HttpStatusCode.NotFound, order.StatusCode);
        await AssertEmptyBodyAsync(order);
        Assert.Equal(HttpStatusCode.NotFound, operation.StatusCode);
        await AssertEmptyBodyAsync(operation);
    }

    [Fact]
    public async Task Two_sessions_racing_on_a_new_cart_id_leave_exactly_one_owner()
    {
        var a = await factory.CreateSessionBrowserAsync();
        var b = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();

        var responses = await Task.WhenAll(
            a.PostLineAsync(cartId, LineBody(Widget)),
            b.PostLineAsync(cartId, LineBody(Gadget, name: "Gadget", sku: "G-1")));

        // Either session may win. The loser gets 404 at acceptance (it already sees the winner's cart)
        // or 202 followed by a failed operation; both are valid outcomes.
        var clients = new[] { a, b };
        var accepted = new AcceptedResult?[2];
        for (var i = 0; i < 2; i++)
        {
            Assert.Contains(responses[i].StatusCode, new[] { HttpStatusCode.Accepted, HttpStatusCode.NotFound });
            if (responses[i].StatusCode == HttpStatusCode.Accepted)
                accepted[i] = await ReadAcceptedAsync(responses[i]);
        }
        Assert.Contains(accepted, x => x is not null);

        var ops = new OperationResult?[2];
        for (var i = 0; i < 2; i++)
            if (accepted[i] is { } acc)
                ops[i] = await ApiFactory.WaitForOperationAsync(clients[i], acc.OperationId);

        var carts = new[] { await a.GetCartAsync(cartId), await b.GetCartAsync(cartId) };
        var owners = Enumerable.Range(0, 2).Where(i => carts[i].Lines.Count == 1).ToList();
        var owner = Assert.Single(owners);
        var other = 1 - owner;

        Assert.Empty(carts[other].Lines);
        Assert.Equal("completed", ops[owner]?.Status);
        if (ops[other] is { } otherOp)
            Assert.Equal("failed", otherOp.Status);
        var rows = await factory.WithDbAsync(db => db.Carts.CountAsync(c => c.Id == cartId));
        Assert.Equal(1, rows);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using uShopDemo.Api.Data;

namespace uShopDemo.Api.Tests;

/// <summary>Body of an accepted write.</summary>
public sealed record AcceptedResult(Guid OperationId, string? OrderNumber);

/// <summary>Body of <c>GET /orders/{n}</c>; members not set for the status are null.</summary>
public sealed record OrderResult(
    string OrderNumber, string Status, string? Reason, string? Name, string? Email, string? Address,
    DateTime? OrderedUtc, List<CartLineResult>? Lines, int? ItemCount, decimal? Total);

public static class ApiHelpers
{
    public static readonly Guid Widget = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Gadget = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static object LineBody(Guid productKey, int quantity = 1, string name = "Widget", string sku = "W-1", decimal price = 9.99m) =>
        new { productKey, name, sku, unitPrice = price, quantity };

    public static object CheckoutBody() =>
        new { name = "Ada Lovelace", email = "ada@example.test", address = "1 Analytical Way", cardNumber = CardNumber };

    public const string CardNumber = "4111111111111111";

    public static StringContent Json(string json) => Content(json, "application/json");

    public static StringContent Content(string body, string mediaType) => new(body, Encoding.UTF8, mediaType);

    public static Task<HttpResponseMessage> PostLineAsync(this HttpClient c, Guid cartId, object body) =>
        c.PostAsJsonAsync($"/carts/{cartId}/lines", body);

    public static Task<HttpResponseMessage> CheckoutAsync(this HttpClient c, Guid cartId, object? body = null) =>
        c.PostAsJsonAsync($"/carts/{cartId}/checkout", body ?? CheckoutBody());

    public static async Task<AcceptedResult> ReadAcceptedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AcceptedResult>();
        Assert.NotNull(body);
        return body;
    }

    /// <summary>Adds a line, requires 202 and waits for the operation, returning its final state.</summary>
    public static async Task<OperationResult> AddAndWaitAsync(
        this HttpClient c, Guid cartId, Guid productKey, int quantity = 1, string name = "Widget", string sku = "W-1", decimal price = 9.99m)
    {
        var accepted = await ReadAcceptedAsync(await c.PostLineAsync(cartId, LineBody(productKey, quantity, name, sku, price)));
        return await ApiFactory.WaitForOperationAsync(c, accepted.OperationId);
    }

    public static async Task AddCompletedAsync(
        this HttpClient c, Guid cartId, Guid productKey, int quantity = 1, string name = "Widget", string sku = "W-1", decimal price = 9.99m)
    {
        var op = await c.AddAndWaitAsync(cartId, productKey, quantity, name, sku, price);
        Assert.Equal("completed", op.Status);
    }

    public static async Task<CartResult> GetCartAsync(this HttpClient c, Guid cartId)
    {
        var response = await c.GetAsync($"/carts/{cartId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<CartResult>();
        Assert.NotNull(cart);
        return cart;
    }

    public static async Task<OrderResult> GetOrderAsync(this HttpClient c, string orderNumber)
    {
        var response = await c.GetAsync($"/orders/{orderNumber}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResult>();
        Assert.NotNull(order);
        return order;
    }

    /// <summary>
    /// Barrier: the queue has one reader and is FIFO, so once an add to a throwaway cart completes, everything
    /// accepted before it has been processed.
    /// </summary>
    public static async Task DrainQueueAsync(this HttpClient c) =>
        await c.AddCompletedAsync(Guid.NewGuid(), Widget);

    public static async Task AssertEmptyBodyAsync(HttpResponseMessage response) =>
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());

    /// <summary>A client without a cookie container (cookies are sent by hand) so headers can be inspected and tampered with. Owned (disposed) by the factory.</summary>
    public static HttpClient CreateRawClient(this ApiFactory factory, string baseAddress = "https://localhost") =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(baseAddress),
            HandleCookies = false,
            AllowAutoRedirect = false,
        });

    /// <summary>Calls <c>POST /session</c> on a raw client and returns the full Set-Cookie header value.</summary>
    public static async Task<string> IssueSessionCookieAsync(HttpClient raw, string? cookieHeader = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/session");
        if (cookieHeader is not null)
            request.Headers.Add("Cookie", cookieHeader);
        var response = await raw.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return Assert.Single(response.Headers.GetValues("Set-Cookie"));
    }

    /// <summary>The <c>name=value</c> part of a Set-Cookie header.</summary>
    public static string CookiePair(string setCookie) => setCookie.Split(';')[0];

    public static async Task<T> WithDbAsync<T>(this ApiFactory factory, Func<CartDbContext, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<CartDbContext>());
    }

    public static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}

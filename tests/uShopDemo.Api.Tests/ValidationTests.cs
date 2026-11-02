using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using uShopDemo.Api.Sessions;
using static uShopDemo.Api.Tests.ApiHelpers;

namespace uShopDemo.Api.Tests;

public sealed class ValidationTests : IDisposable
{
    private const string Key = "11111111-1111-1111-1111-111111111111";

    private readonly ApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    public static TheoryData<string, string> InvalidAdds => new()
    {
        { "quantity 0", $$"""{"productKey":"{{Key}}","name":"W","sku":"S","unitPrice":1,"quantity":0}""" },
        { "quantity 100", $$"""{"productKey":"{{Key}}","name":"W","sku":"S","unitPrice":1,"quantity":100}""" },
        { "quantity missing", $$"""{"productKey":"{{Key}}","name":"W","sku":"S","unitPrice":1}""" },
        { "productKey missing", """{"name":"W","sku":"S","unitPrice":1,"quantity":1}""" },
        { "name missing", $$"""{"productKey":"{{Key}}","sku":"S","unitPrice":1,"quantity":1}""" },
        { "sku missing", $$"""{"productKey":"{{Key}}","name":"W","unitPrice":1,"quantity":1}""" },
        { "price negative", $$"""{"productKey":"{{Key}}","name":"W","sku":"S","unitPrice":-0.01,"quantity":1}""" },
        { "price over 1,000,000", $$"""{"productKey":"{{Key}}","name":"W","sku":"S","unitPrice":1000000.01,"quantity":1}""" },
    };

    public static TheoryData<string, string> InvalidCheckouts => new()
    {
        { "email missing", """{"name":"A","address":"B","cardNumber":"4111"}""" },
        { "email invalid", """{"name":"A","email":"not-an-email","address":"B","cardNumber":"4111"}""" },
        { "cardNumber empty", """{"name":"A","email":"a@b.test","address":"B","cardNumber":""}""" },
        { "cardNumber missing", """{"name":"A","email":"a@b.test","address":"B"}""" },
        { "name missing", """{"email":"a@b.test","address":"B","cardNumber":"4111"}""" },
        { "address missing", """{"name":"A","email":"a@b.test","cardNumber":"4111"}""" },
    };

    [Theory]
    [MemberData(nameof(InvalidAdds))]
    public async Task Invalid_add_is_400_and_creates_no_cart(string _, string json)
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();

        var response = await client.PostAsync($"/carts/{cartId}/lines", Json(json));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await client.DrainQueueAsync();
        Assert.Equal(0, await factory.WithDbAsync(db => db.Carts.CountAsync(c => c.Id == cartId)));
        Assert.Empty((await client.GetCartAsync(cartId)).Lines);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public async Task Invalid_update_is_400_and_cart_is_unchanged(int quantity)
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget, 2);

        var response = await client.PutAsJsonAsync($"/carts/{cartId}/lines/{Widget}", new { quantity });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await client.DrainQueueAsync();
        Assert.Equal(2, Assert.Single((await client.GetCartAsync(cartId)).Lines).Quantity);
    }

    [Theory]
    [MemberData(nameof(InvalidCheckouts))]
    public async Task Invalid_checkout_is_400_without_order_number_and_cart_is_unchanged(string _, string json)
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget, 2);

        var response = await client.PostAsync($"/carts/{cartId}/checkout", Json(json));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("orderNumber", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        await client.DrainQueueAsync();
        Assert.Equal(2, Assert.Single((await client.GetCartAsync(cartId)).Lines).Quantity);
        Assert.Equal(0, await factory.WithDbAsync(db => db.Carts.CountAsync(c => c.OrderNumber != null)));
    }

    [Fact]
    public async Task Boundary_values_are_accepted()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();

        var op = await client.AddAndWaitAsync(cartId, Widget, quantity: 99, price: 1_000_000m);

        Assert.Equal("completed", op.Status);
        Assert.Equal(99, Assert.Single((await client.GetCartAsync(cartId)).Lines).Quantity);
    }

    [Fact]
    public async Task Missing_field_returns_validation_problem_with_camel_case_key()
    {
        var client = await factory.CreateSessionBrowserAsync();

        var response = await client.PostAsync($"/carts/{Guid.NewGuid()}/lines",
            Json($$"""{"productKey":"{{Key}}","name":"W","sku":"S","unitPrice":1}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, json.RootElement.GetProperty("errors").GetProperty("quantity").ValueKind);
    }

    [Fact]
    public async Task With_a_session_malformed_json_is_400()
    {
        var client = await factory.CreateSessionBrowserAsync();

        var response = await client.PostAsync($"/carts/{Guid.NewGuid()}/lines", Json("{ not json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task With_a_session_text_plain_is_415()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var body = Content("{}", "text/plain");

        var response = await client.PostAsync($"/carts/{Guid.NewGuid()}/lines", body);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task With_a_session_plus_json_content_type_is_415_and_queues_nothing()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        var body = Content(
            $$"""{"productKey":"{{Key}}","name":"W","sku":"S","unitPrice":1,"quantity":1}""",
            "application/vnd.api+json");

        var response = await client.PostAsync($"/carts/{cartId}/lines", body);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        await client.DrainQueueAsync();
        Assert.Equal(0, await factory.WithDbAsync(db => db.Carts.CountAsync(c => c.Id == cartId)));
    }

    [Fact]
    public async Task Non_json_checkout_and_update_are_415()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();

        var checkout = await client.PostAsync($"/carts/{cartId}/checkout", Content("x", "text/plain"));
        var update = await client.PutAsync($"/carts/{cartId}/lines/{Widget}", Content("x", "application/x-www-form-urlencoded"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, checkout.StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, update.StatusCode);
    }

    [Fact]
    public async Task Without_a_session_text_plain_is_401()
    {
        var client = factory.CreateBrowser();
        var body = Content("{}", "text/plain");

        var response = await client.PostAsync($"/carts/{Guid.NewGuid()}/lines", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Without_a_session_malformed_json_is_401()
    {
        var client = factory.CreateBrowser();

        var response = await client.PostAsync($"/carts/{Guid.NewGuid()}/lines", Json("{ not json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static TheoryData<string, string> UnauthenticatedBodyRequests()
    {
        var data = new TheoryData<string, string>();
        foreach (var target in new[] { "POST lines", "PUT line", "POST checkout" })
            foreach (var type in new[] { "text/plain", "application/x-www-form-urlencoded", "multipart/form-data" })
                data.Add(target, type);
        return data;
    }

    [Theory]
    [MemberData(nameof(UnauthenticatedBodyRequests))]
    public async Task Without_a_session_any_content_type_is_401_with_empty_body_and_queues_nothing(string target, string mediaType)
    {
        var client = factory.CreateBrowser();
        var cartId = Guid.NewGuid();
        var url = target switch
        {
            "POST lines" => $"/carts/{cartId}/lines",
            "PUT line" => $"/carts/{cartId}/lines/{Widget}",
            _ => $"/carts/{cartId}/checkout",
        };
        using var request = new HttpRequestMessage(target.StartsWith("PUT") ? HttpMethod.Put : HttpMethod.Post, url)
        {
            Content = Content("x", mediaType),
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertEmptyBodyAsync(response);
        await (await factory.CreateSessionBrowserAsync()).DrainQueueAsync();
        Assert.Equal(0, await factory.WithDbAsync(db => db.Carts.CountAsync(c => c.Id == cartId)));
    }

    [Fact]
    public async Task Without_a_session_delete_is_401()
    {
        var client = factory.CreateBrowser();

        var response = await client.DeleteAsync($"/carts/{Guid.NewGuid()}/lines/{Widget}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertEmptyBodyAsync(response);
    }

    /// <summary>
    /// Body endpoints are identified as session-protected endpoints that either carry an inferred IAcceptsMetadata or
    /// are POST/PUT. Using both (union) means an endpoint is still checked if body inference changes, and a body
    /// endpoint whose accepts metadata is missing altogether is caught by the "last accepts" assertion.
    /// </summary>
    [Fact]
    public void Every_session_protected_body_endpoint_accepts_any_content_type_last()
    {
        var endpoints = factory.Services.GetServices<EndpointDataSource>()
            .SelectMany(s => s.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<RequiresSessionMetadata>() is not null)
            .ToList();
        var bodyEndpoints = endpoints
            .Where(e => e.Metadata.GetMetadata<IAcceptsMetadata>() is not null
                || e.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Any(m => m is "POST" or "PUT"))
            .ToList();

        Assert.True(bodyEndpoints.Count >= 3, "Expected at least the add, update and checkout endpoints.");
        foreach (var endpoint in bodyEndpoints)
        {
            var last = endpoint.Metadata.GetOrderedMetadata<IAcceptsMetadata>().LastOrDefault();
            Assert.True(last is { ContentTypes.Count: 0 }, $"{endpoint.DisplayName}: last IAcceptsMetadata must have empty ContentTypes.");
        }
    }
}

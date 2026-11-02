using System.Net;
using System.Net.Http.Json;

namespace uShopDemo.Api.Tests;

public sealed class SmokeTests : IDisposable
{
    private readonly ApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task New_cart_is_empty()
    {
        var client = await factory.CreateSessionBrowserAsync();

        var response = await client.GetAsync($"/carts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<CartResult>();
        Assert.NotNull(cart);
        Assert.Empty(cart.Lines);
    }

    [Fact]
    public async Task Health_is_ok()
    {
        var response = await factory.CreateBrowser().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace uShopDemo.Api.Tests;

public sealed class OpenApiTests : IDisposable
{
    private readonly ApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Theory]
    [InlineData("/session", "post")]
    [InlineData("/carts/{cartId}", "get")]
    [InlineData("/carts/{cartId}/lines", "post")]
    [InlineData("/carts/{cartId}/lines/{productKey}", "put")]
    [InlineData("/carts/{cartId}/lines/{productKey}", "delete")]
    [InlineData("/carts/{cartId}/checkout", "post")]
    [InlineData("/operations/{operationId}", "get")]
    [InlineData("/orders/{orderNumber}", "get")]
    [InlineData("/health", "get")]
    public async Task Document_lists_endpoint(string path, string method)
    {
        using var doc = await GetDocumentAsync();
        Assert.True(doc.RootElement.GetProperty("paths").TryGetProperty(path, out var item), $"Missing path {path}");
        Assert.True(item.TryGetProperty(method, out _), $"Missing {method} {path}");
    }

    [Theory]
    [InlineData("/carts/{cartId}/lines", "post")]
    [InlineData("/carts/{cartId}/lines/{productKey}", "put")]
    [InlineData("/carts/{cartId}/checkout", "post")]
    public async Task Write_endpoints_document_a_json_request_body(string path, string method)
    {
        using var doc = await GetDocumentAsync();
        var content = doc.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method)
            .GetProperty("requestBody").GetProperty("content");
        Assert.True(content.TryGetProperty("application/json", out var json), "No application/json request body");
        Assert.True(json.TryGetProperty("schema", out _));
        Assert.False(content.TryGetProperty("*/*", out _));
    }

    [Theory]
    [InlineData("/carts/{cartId}", "get", "401")]
    [InlineData("/carts/{cartId}/lines", "post", "401")]
    [InlineData("/carts/{cartId}/lines", "post", "404")]
    [InlineData("/carts/{cartId}/lines", "post", "415")]
    [InlineData("/carts/{cartId}/lines/{productKey}", "put", "401")]
    [InlineData("/carts/{cartId}/lines/{productKey}", "put", "404")]
    [InlineData("/carts/{cartId}/lines/{productKey}", "put", "415")]
    [InlineData("/carts/{cartId}/lines/{productKey}", "delete", "401")]
    [InlineData("/carts/{cartId}/lines/{productKey}", "delete", "404")]
    [InlineData("/carts/{cartId}/checkout", "post", "401")]
    [InlineData("/carts/{cartId}/checkout", "post", "404")]
    [InlineData("/carts/{cartId}/checkout", "post", "415")]
    [InlineData("/operations/{operationId}", "get", "401")]
    [InlineData("/operations/{operationId}", "get", "404")]
    [InlineData("/orders/{orderNumber}", "get", "401")]
    [InlineData("/orders/{orderNumber}", "get", "404")]
    [InlineData("/session", "post", "403")]
    public async Task Bodiless_error_responses_declare_no_content(string path, string method, string status)
    {
        using var doc = await GetDocumentAsync();
        var response = doc.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method)
            .GetProperty("responses").GetProperty(status);
        Assert.False(response.TryGetProperty("content", out _), $"{method} {path} {status} declares content");
    }

    [Theory]
    [InlineData("/carts/{cartId}/lines", "post", "400")]
    [InlineData("/carts/{cartId}/checkout", "post", "400")]
    [InlineData("/carts/{cartId}/checkout", "post", "503")]
    public async Task Problem_responses_declare_problem_json(string path, string method, string status)
    {
        using var doc = await GetDocumentAsync();
        var content = doc.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method)
            .GetProperty("responses").GetProperty(status).GetProperty("content");
        Assert.True(content.TryGetProperty("application/problem+json", out _));
    }

    [Fact]
    public async Task Scalar_ui_responds_without_a_session()
    {
        var browser = factory.CreateBrowser();
        var redirect = await browser.GetAsync("/scalar");
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);

        var response = await browser.GetAsync(redirect.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    public async Task Document_and_scalar_are_absent_outside_development(string path)
    {
        using var production = factory.WithWebHostBuilder(b => b.UseEnvironment("Production"));
        var response = await production.CreateClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<JsonDocument> GetDocumentAsync()
    {
        var response = await factory.CreateBrowser().GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }
}

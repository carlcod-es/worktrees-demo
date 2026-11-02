using System.Net;
using static uShopDemo.Api.Tests.ApiHelpers;

namespace uShopDemo.Api.Tests;

public sealed class CorsTests : IDisposable
{
    private readonly ApiFactory factory = new();
    private ApiFactory? unconfigured;

    public void Dispose()
    {
        factory.Dispose();
        unconfigured?.Dispose();
    }

    private static async Task<HttpResponseMessage> PreflightAsync(ApiFactory f, string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, $"/carts/{Guid.NewGuid()}/lines");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        using var client = f.CreateRawClient();
        return await client.SendAsync(request);
    }

    private static bool HasCorsHeaders(HttpResponseMessage response) =>
        response.Headers.Any(h => h.Key.StartsWith("Access-Control-", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public async Task Preflight_from_the_site_origin_is_allowed_with_credentials()
    {
        var response = await PreflightAsync(factory, ApiFactory.WebOrigin);

        Assert.True(response.IsSuccessStatusCode, $"Status {response.StatusCode}");
        Assert.Equal(ApiFactory.WebOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
        Assert.Contains("POST", string.Join(',', response.Headers.GetValues("Access-Control-Allow-Methods")));
    }

    [Fact]
    public async Task Preflight_from_another_origin_gets_no_cors_headers()
    {
        var response = await PreflightAsync(factory, "https://evil.test");

        Assert.False(HasCorsHeaders(response));
    }

    [Fact]
    public async Task Preflight_with_no_configured_origin_gets_no_allow_origin()
    {
        unconfigured = new ApiFactory(configureWebOrigin: false);

        var response = await PreflightAsync(unconfigured, "https://evil.test");
        var siteResponse = await PreflightAsync(unconfigured, ApiFactory.WebOrigin);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(siteResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Actual_request_from_the_site_origin_gets_cors_headers()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/carts/{Guid.NewGuid()}");
        request.Headers.Add("Origin", ApiFactory.WebOrigin);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ApiFactory.WebOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
    }
}

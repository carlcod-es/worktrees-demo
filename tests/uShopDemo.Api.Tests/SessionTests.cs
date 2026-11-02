using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static uShopDemo.Api.Tests.ApiHelpers;

namespace uShopDemo.Api.Tests;

public sealed class SessionTests : IDisposable
{
    private const string CookieName = "__Host-ushop-session-443";

    private readonly ApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task Session_cookie_has_the_required_attributes()
    {
        var setCookie = await IssueSessionCookieAsync(factory.CreateRawClient());
        var lower = setCookie.ToLowerInvariant();

        Assert.StartsWith(CookieName + "=", setCookie);
        Assert.Contains("httponly", lower);
        Assert.Contains("secure", lower);
        Assert.Contains("samesite=strict", lower);
        Assert.Contains("path=/", lower);
        Assert.Contains($"max-age={(int)TimeSpan.FromDays(30).TotalSeconds}", lower);
        Assert.DoesNotContain("domain", lower);
    }

    [Fact]
    public async Task Session_keys_are_persisted_to_the_database()
    {
        await IssueSessionCookieAsync(factory.CreateRawClient());

        var keys = await factory.WithDbAsync(db => db.DataProtectionKeys.CountAsync());

        Assert.True(keys >= 1);
        var table = await factory.WithDbAsync(db => Task.FromResult(db.Model.FindEntityType(typeof(DataProtectionKey))!.GetTableName()));
        Assert.Equal("ushopDataProtectionKeys", table);
    }

    [Fact]
    public async Task Post_session_with_a_valid_cookie_keeps_the_same_session()
    {
        var client = await factory.CreateSessionBrowserAsync();
        var cartId = Guid.NewGuid();
        await client.AddCompletedAsync(cartId, Widget);

        var renewal = await client.PostAsync("/session", content: null);

        Assert.Equal(HttpStatusCode.NoContent, renewal.StatusCode);
        Assert.Single((await client.GetCartAsync(cartId)).Lines);
    }

    [Fact]
    public async Task Api_restart_keeps_existing_sessions_valid_and_their_carts_readable()
    {
        var database = $"ushop-restart-{Guid.NewGuid():N}";
        using var holder = new SqliteConnection(ApiFactory.ConnectionStringFor(database));
        holder.Open();
        var cartId = Guid.NewGuid();
        string cookie;
        using (var first = new ApiFactory(databaseName: database))
        {
            var raw = first.CreateRawClient();
            cookie = CookiePair(await IssueSessionCookieAsync(raw));
            using var add = new HttpRequestMessage(HttpMethod.Post, $"/carts/{cartId}/lines")
            {
                Content = Json(System.Text.Json.JsonSerializer.Serialize(LineBody(Widget))),
            };
            add.Headers.Add("Cookie", cookie);
            var accepted = await ReadAcceptedAsync(await raw.SendAsync(add));
            await WaitForCompletedAsync(raw, accepted.OperationId, cookie);
        }

        using var second = new ApiFactory(databaseName: database);
        var client = second.CreateRawClient();

        using var read = new HttpRequestMessage(HttpMethod.Get, $"/carts/{cartId}");
        read.Headers.Add("Cookie", cookie);
        var readResponse = await client.SendAsync(read);
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        var cart = await readResponse.Content.ReadFromJsonAsync<CartResult>();
        Assert.Equal(Widget, Assert.Single(cart!.Lines).ProductKey);

        var renewed = await IssueSessionCookieAsync(client, cookie);
        Assert.Equal(cookie.Split('=')[0], CookiePair(renewed).Split('=')[0]);
        using var again = new HttpRequestMessage(HttpMethod.Get, $"/carts/{cartId}");
        again.Headers.Add("Cookie", CookiePair(renewed));
        Assert.Single((await (await client.SendAsync(again)).Content.ReadFromJsonAsync<CartResult>())!.Lines);
    }

    private static async Task WaitForCompletedAsync(HttpClient raw, Guid operationId, string cookie)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/operations/{operationId}");
            request.Headers.Add("Cookie", cookie);
            var body = await (await raw.SendAsync(request)).Content.ReadFromJsonAsync<OperationResult>();
            if (body!.Status == "completed")
                return;
            Assert.Contains(body.Status, new[] { "queued" });
            await Task.Delay(25);
        }
        Assert.Fail("Operation did not complete in time.");
    }

    [Fact]
    public async Task Cookie_name_carries_the_port_so_two_instances_on_one_host_do_not_share_sessions()
    {
        var on5001 = factory.CreateRawClient("https://localhost:5001");
        var on443 = factory.CreateRawClient();

        var setCookie = await IssueSessionCookieAsync(on5001);
        var cookie443 = CookiePair(await IssueSessionCookieAsync(on443));

        Assert.StartsWith("__Host-ushop-session-5001=", setCookie);
        Assert.StartsWith(CookieName + "=", cookie443);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/carts/{Guid.NewGuid()}");
        request.Headers.Add("Cookie", cookie443);
        Assert.Equal(HttpStatusCode.Unauthorized, (await on5001.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Post_session_without_a_cookie_issues_distinct_sessions()
    {
        var first = CookiePair(await IssueSessionCookieAsync(factory.CreateRawClient()));
        var second = CookiePair(await IssueSessionCookieAsync(factory.CreateRawClient()));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task No_session_cookie_is_401_with_empty_body_and_queues_nothing()
    {
        var client = factory.CreateBrowser();
        var cartId = Guid.NewGuid();

        var responses = new[]
        {
            await client.GetAsync($"/carts/{cartId}"),
            await client.PostLineAsync(cartId, LineBody(Widget)),
            await client.GetAsync($"/operations/{Guid.NewGuid()}"),
            await client.GetAsync("/orders/US-000000-00000"),
        };

        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            await AssertEmptyBodyAsync(response);
        }
        var rows = await factory.WithDbAsync(db => db.Carts.CountAsync(c => c.Id == cartId));
        Assert.Equal(0, rows);
    }

    [Fact]
    public async Task Tampered_cookie_is_401_and_post_session_issues_a_new_one()
    {
        var raw = factory.CreateRawClient();
        var original = CookiePair(await IssueSessionCookieAsync(raw));
        var tampered = Tamper(original);

        using var read = new HttpRequestMessage(HttpMethod.Get, $"/carts/{Guid.NewGuid()}");
        read.Headers.Add("Cookie", tampered);
        var readResponse = await raw.SendAsync(read);
        Assert.Equal(HttpStatusCode.Unauthorized, readResponse.StatusCode);
        await AssertEmptyBodyAsync(readResponse);

        var renewed = await IssueSessionCookieAsync(raw, tampered);
        Assert.StartsWith(CookieName + "=", renewed);
        Assert.NotEqual(tampered, CookiePair(renewed));
        Assert.NotEqual(original, CookiePair(renewed));
    }

    [Fact]
    public async Task Origin_that_is_not_the_site_gets_403_without_a_cookie()
    {
        var response = await PostSessionAsync("https://evil.test");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Site_origin_gets_204_with_a_cookie()
    {
        var response = await PostSessionAsync(ApiFactory.WebOrigin);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task No_origin_gets_204()
    {
        var response = await PostSessionAsync(null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private Task<HttpResponseMessage> PostSessionAsync(string? origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/session");
        if (origin is not null)
            request.Headers.Add("Origin", origin);
        return factory.CreateRawClient().SendAsync(request);
    }

    /// <summary>Changes one character in the middle of the cookie value.</summary>
    private static string Tamper(string pair)
    {
        var chars = pair.ToCharArray();
        var index = pair.IndexOf('=') + 1 + (pair.Length - pair.IndexOf('=') - 1) / 2;
        chars[index] = chars[index] == 'A' ? 'B' : 'A';
        return new string(chars);
    }
}

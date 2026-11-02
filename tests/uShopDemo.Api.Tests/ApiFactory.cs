using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using uShopDemo.Api.Data;

namespace uShopDemo.Api.Tests;

/// <summary>
/// In-process API over a named shared-cache SQLite in-memory database (ADR-0014). A keep-alive connection holds the
/// database open for the factory's lifetime; each DbContext opens its own connection, so concurrent requests and the
/// hosted worker are safe. Create one factory per test class (or per test when tests need a pristine database); each
/// factory owns its own uniquely named database, so nothing leaks between factories.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string WebOrigin = "https://shop.test";

    private readonly string connectionString;
    private readonly SqliteConnection keepAlive;
    private readonly bool configureWebOrigin;

    /// <summary>Connection string of the shared-cache in-memory database called <paramref name="databaseName"/>.</summary>
    public static string ConnectionStringFor(string databaseName) =>
        $"DataSource=file:{databaseName}?mode=memory&cache=shared";

    /// <param name="configureWebOrigin">False leaves <c>USHOP_WEB_ORIGIN</c> empty (no cross-origin access).</param>
    /// <param name="databaseName">
    /// Name of the in-memory database; default is a fresh unique one. Pass a name to point a second factory at the
    /// same data (API restart): something else must hold that database open while the first factory is disposed.
    /// </param>
    public ApiFactory(bool configureWebOrigin = true, string? databaseName = null)
    {
        this.configureWebOrigin = configureWebOrigin;
        connectionString = ConnectionStringFor(databaseName ?? $"ushop-{Guid.NewGuid():N}");
        keepAlive = new SqliteConnection(connectionString);
        keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting so the values are visible to Program's early builder.Configuration reads.
        builder.UseSetting("ConnectionStrings:umbracoDbDSN", "Server=unused;Database=unused;User Id=x;Password=x;TrustServerCertificate=true");
        builder.UseSetting("USHOP_WEB_ORIGIN", configureWebOrigin ? WebOrigin : "");

        builder.ConfigureTestServices(services =>
        {
            // Drop everything Aspire's AddSqlServerDbContext registered for the context (options, pool, lease, the
            // context itself); a second provider on top fails with "multiple database providers registered".
            // The Aspire health check is kept: it resolves CartDbContext, so it runs against SQLite.
            var stale = services.Where(d => IsCartContextRegistration(d.ServiceType)).ToList();
            foreach (var descriptor in stale)
                services.Remove(descriptor);

            services.AddDbContext<CartDbContext>(o => o.UseSqlite(connectionString));
        });
    }

    private static bool IsCartContextRegistration(Type type) =>
        type == typeof(CartDbContext)
        || type == typeof(DbContextOptions)
        || (type.IsGenericType && type.GetGenericArguments().Contains(typeof(CartDbContext)));

    // Clients from CreateBrowser, CreateSessionBrowserAsync and CreateRawClient are created through
    // WebApplicationFactory.CreateClient, which tracks them and disposes them with the factory. Tests do not dispose them.

    /// <summary>One "browser": its own cookie container, base address https://localhost so the session cookie name ends -443. Owned (disposed) by the factory.</summary>
    public HttpClient CreateBrowser() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
            AllowAutoRedirect = false,
        });

    /// <summary>A browser that has already called <c>POST /session</c> (must return 204). Owned (disposed) by the factory.</summary>
    public async Task<HttpClient> CreateSessionBrowserAsync()
    {
        var client = CreateBrowser();
        var response = await client.PostAsync("/session", content: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return client;
    }

    /// <summary>
    /// Polls <c>GET /operations/{id}</c> (use the client that created the operation) until it is completed or failed.
    /// Fails the test if that does not happen within <paramref name="timeout"/> (default 5 s).
    /// </summary>
    public static async Task<OperationResult> WaitForOperationAsync(HttpClient client, Guid operationId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        string lastStatus = "unknown";
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/operations/{operationId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<OperationResult>();
            Assert.NotNull(body);
            lastStatus = body.Status;
            Assert.True(lastStatus is "queued" or "completed" or "failed", $"Unexpected operation status '{lastStatus}'.");
            if (lastStatus is "completed" or "failed")
                return body;
            await Task.Delay(25);
        }
        Assert.Fail($"Operation {operationId} did not finish in time; last status '{lastStatus}'.");
        return null!; // unreachable: Assert.Fail throws
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            keepAlive.Dispose();
    }
}

/// <summary>Body of <c>GET /operations/{id}</c>.</summary>
public sealed record OperationResult(Guid OperationId, string Status, string? Error);

/// <summary>Body of <c>GET /carts/{id}</c>.</summary>
public sealed record CartResult(Guid CartId, List<CartLineResult> Lines, int ItemCount, decimal Total);

public sealed record CartLineResult(Guid ProductKey, string Name, string Sku, decimal UnitPrice, int Quantity, decimal LineTotal);

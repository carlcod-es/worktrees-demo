using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Persistence.EFCore.Scoping;
using Umbraco.Extensions;
using uShopDemo.Web.Shop.Data;

namespace uShopDemo.Web.Shop;

public record CartLineView(int Id, Guid ProductKey, string ProductName, string Sku, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public record CartView(IReadOnlyList<CartLineView> Lines)
{
    public static readonly CartView Empty = new([]);
    public int ItemCount => Lines.Sum(l => l.Quantity);
    public decimal Total => Lines.Sum(l => l.LineTotal);
    public bool IsEmpty => Lines.Count == 0;
}

public record OrderView(string OrderNumber, string CustomerName, string Email, string Address, DateTime OrderedUtc, decimal Total, IReadOnlyList<CartLineView> Lines);

/// <summary>Current visitor's cart and orders. Views read through this; only the surface controller mutates.</summary>
public interface ICartService
{
    Task<CartView> GetCurrentAsync();
    Task AddAsync(Guid productKey, int quantity);
    Task UpdateAsync(int lineId, int quantity);
    Task RemoveAsync(int lineId);
    /// <summary>Converts the open cart to an order and clears the cookie. Null when there is nothing to order.</summary>
    Task<string?> PlaceOrderAsync(string customerName, string email, string address);
    Task<OrderView?> GetOrderAsync(string orderNumber);
}

public static class Money
{
    public static string Format(decimal amount) => amount.ToString("$#,##0.00", CultureInfo.InvariantCulture);
}

public class CartService(
    IEFCoreScopeProvider<ShopDbContext> scopes,
    IHttpContextAccessor http,
    IUmbracoContextAccessor umbracoContext) : ICartService
{
    public const string CookieName = "ushop_cart";
    public const int MaxQuantity = 99;

    private CartView? _current;

    public async Task<CartView> GetCurrentAsync()
    {
        if (_current is not null) return _current;
        var id = CookieCartId();
        if (id is null) return _current = CartView.Empty;

        var lines = await Run(db => db.CartLines
            .Where(l => l.CartId == id && db.Carts.Any(c => c.Id == id && c.Status == CartStatus.Open))
            .OrderBy(l => l.Id)
            .Select(l => new CartLineView(l.Id, l.ProductKey, l.ProductName, l.Sku, l.UnitPrice, l.Quantity))
            .ToListAsync());
        return _current = new CartView(lines);
    }

    public async Task AddAsync(Guid productKey, int quantity)
    {
        quantity = Math.Clamp(quantity, 1, MaxQuantity);
        var product = umbracoContext.GetRequiredUmbracoContext().Content?.GetById(productKey);
        if (product is null || product.ContentType.Alias != "product")
            throw new InvalidOperationException($"Unknown product {productKey}");

        var cartId = await EnsureOpenCartAsync();
        await Run(async db =>
        {
            var line = await db.CartLines.FirstOrDefaultAsync(l => l.CartId == cartId && l.ProductKey == productKey);
            if (line is null)
            {
                db.CartLines.Add(new CartLine
                {
                    CartId = cartId,
                    ProductKey = productKey,
                    ProductName = product.Value<string>("title") ?? product.Name,
                    Sku = product.Value<string>("sku") ?? "",
                    UnitPrice = product.Value<decimal>("price"),
                    Quantity = quantity,
                });
            }
            else
            {
                line.Quantity = Math.Min(line.Quantity + quantity, MaxQuantity);
            }
            return await db.SaveChangesAsync();
        });
        _current = null;
    }

    public async Task UpdateAsync(int lineId, int quantity)
    {
        if (quantity <= 0)
        {
            await RemoveAsync(lineId);
            return;
        }
        var cartId = CookieCartId();
        if (cartId is null) return;
        await Run(db => OpenLines(db, cartId.Value).Where(l => l.Id == lineId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.Quantity, Math.Min(quantity, MaxQuantity))));
        _current = null;
    }

    public async Task RemoveAsync(int lineId)
    {
        var cartId = CookieCartId();
        if (cartId is null) return;
        await Run(db => OpenLines(db, cartId.Value).Where(l => l.Id == lineId).ExecuteDeleteAsync());
        _current = null;
    }

    public async Task<string?> PlaceOrderAsync(string customerName, string email, string address)
    {
        var cartId = CookieCartId();
        if (cartId is null) return null;

        var orderNumber = await Run(async db =>
        {
            var cart = await db.Carts.Include(c => c.Lines)
                .FirstOrDefaultAsync(c => c.Id == cartId && c.Status == CartStatus.Open);
            if (cart is null || cart.Lines.Count == 0) return null;

            cart.Status = CartStatus.Ordered;
            cart.OrderNumber = $"US-{DateTime.UtcNow:yyMMdd}-{RandomNumberGenerator.GetInt32(10000, 100000)}";
            cart.CustomerName = customerName.Trim();
            cart.Email = email.Trim();
            cart.Address = address.Trim();
            cart.OrderedUtc = DateTime.UtcNow;
            cart.Total = cart.Lines.Sum(l => l.UnitPrice * l.Quantity);
            await db.SaveChangesAsync();
            return cart.OrderNumber;
        });

        if (orderNumber is not null)
        {
            http.HttpContext?.Response.Cookies.Delete(CookieName);
            _current = CartView.Empty;
        }
        return orderNumber;
    }

    public Task<OrderView?> GetOrderAsync(string orderNumber) =>
        Run(async db => await db.Carts
            .Where(c => c.Status == CartStatus.Ordered && c.OrderNumber == orderNumber)
            .Select(c => new OrderView(
                c.OrderNumber!, c.CustomerName!, c.Email!, c.Address!, c.OrderedUtc!.Value, c.Total!.Value,
                c.Lines.OrderBy(l => l.Id)
                    .Select(l => new CartLineView(l.Id, l.ProductKey, l.ProductName, l.Sku, l.UnitPrice, l.Quantity))
                    .ToList()))
            .FirstOrDefaultAsync());

    private static IQueryable<CartLine> OpenLines(ShopDbContext db, Guid cartId) =>
        db.CartLines.Where(l => l.CartId == cartId && db.Carts.Any(c => c.Id == cartId && c.Status == CartStatus.Open));

    private async Task<Guid> EnsureOpenCartAsync()
    {
        var id = CookieCartId();
        if (id is not null && await Run(db => db.Carts.AnyAsync(c => c.Id == id && c.Status == CartStatus.Open)))
            return id.Value;

        var cart = new Cart { Id = Guid.NewGuid(), CreatedUtc = DateTime.UtcNow, Status = CartStatus.Open };
        await Run(db =>
        {
            db.Carts.Add(cart);
            return db.SaveChangesAsync();
        });
        http.HttpContext?.Response.Cookies.Append(CookieName, cart.Id.ToString(), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
        });
        return cart.Id;
    }

    private Guid? CookieCartId() =>
        Guid.TryParse(http.HttpContext?.Request.Cookies[CookieName], out var id) ? id : null;

    private async Task<T> Run<T>(Func<ShopDbContext, Task<T>> work)
    {
        using var scope = scopes.CreateScope();
        var result = await scope.ExecuteWithContextAsync(work);
        scope.Complete();
        return result;
    }
}

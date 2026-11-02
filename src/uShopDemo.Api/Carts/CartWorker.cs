using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using uShopDemo.Api.Data;

namespace uShopDemo.Api.Carts;

/// <summary>Single reader: applies cart commands one at a time, one DI scope and one save per command.</summary>
public sealed class CartWorker(
    Channel<CartCommand> channel,
    IServiceScopeFactory scopes,
    OperationTracker tracker,
    ILogger<CartWorker> logger) : BackgroundService
{
    /// <summary>Thrown for expected business failures; the message is the operation's error.</summary>
    private sealed class CommandFailedException(string message) : Exception(message);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var command in channel.Reader.ReadAllAsync(stoppingToken))
                await ProcessAsync(command, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ProcessAsync(CartCommand command, CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();
            await ApplyAsync(db, command, ct);
            tracker.MarkCompleted(command.OperationId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            tracker.MarkFailed(command.OperationId, "service stopping");
            throw;
        }
        catch (CommandFailedException ex)
        {
            logger.LogInformation("Cart operation {OperationId} on cart {CartId} failed: {Reason}",
                command.OperationId, command.CartId, ex.Message);
            tracker.MarkFailed(command.OperationId, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cart operation {OperationId} on cart {CartId} failed unexpectedly",
                command.OperationId, command.CartId);
            tracker.MarkFailed(command.OperationId, "unexpected error");
        }
    }

    private async Task ApplyAsync(CartDbContext db, CartCommand command, CancellationToken ct)
    {
        var cart = await db.Carts.Include(c => c.Lines).FirstOrDefaultAsync(c => c.Id == command.CartId, ct);

        if (cart is not null && cart.SessionId != command.SessionId)
            throw new CommandFailedException("cart not found");

        switch (command)
        {
            case AddLine add:
                await AddAsync(db, cart, add, ct);
                break;
            case SetQuantity set:
                var setCart = Require(cart);
                SetLine(db, setCart, set);
                await db.SaveChangesAsync(ct);
                break;
            case RemoveLine remove:
                var removeCart = Require(cart);
                if (removeCart.Lines.FirstOrDefault(l => l.ProductKey == remove.ProductKey) is { } line)
                    db.CartLines.Remove(line);
                await db.SaveChangesAsync(ct);
                break;
            case Checkout checkout:
                await CheckoutAsync(db, cart, checkout, ct);
                break;
            default:
                throw new InvalidOperationException($"Unknown command {command.GetType().Name}");
        }
    }

    private async Task AddAsync(CartDbContext db, Data.Cart? existing, AddLine add, CancellationToken ct)
    {
        var creating = existing is null;
        var cart = existing;
        if (cart is null)
        {
            cart = new Data.Cart
            {
                Id = add.CartId,
                SessionId = add.SessionId,
                CreatedUtc = DateTime.UtcNow,
                Status = CartStatus.Open,
            };
            db.Carts.Add(cart);
        }
        else
        {
            EnsureOpen(cart);
        }

        var line = cart.Lines.FirstOrDefault(l => l.ProductKey == add.ProductKey);
        if (line is not null)
        {
            line.Quantity = Math.Min(CartRules.MaxQuantity, line.Quantity + add.Quantity);
        }
        else
        {
            var newLine = new CartLine
            {
                CartId = cart.Id,
                ProductKey = add.ProductKey,
                ProductName = add.Name,
                Sku = add.Sku,
                UnitPrice = add.UnitPrice,
                Quantity = Math.Min(CartRules.MaxQuantity, add.Quantity),
            };
            if (creating)
                cart.Lines.Add(newLine);
            else
                db.CartLines.Add(newLine);
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (creating)
        {
            logger.LogWarning(ex, "Cart operation {OperationId} on cart {CartId} hit a database update error", add.OperationId, add.CartId);
            // Another session created a cart with this id first.
            throw new CommandFailedException("cart not found");
        }
    }

    private static void SetLine(CartDbContext db, Data.Cart cart, SetQuantity set)
    {
        var line = cart.Lines.FirstOrDefault(l => l.ProductKey == set.ProductKey)
            ?? throw new CommandFailedException("line not in cart");
        if (set.Quantity <= 0)
            db.CartLines.Remove(line);
        else
            line.Quantity = Math.Min(CartRules.MaxQuantity, set.Quantity);
    }

    private async Task CheckoutAsync(CartDbContext db, Data.Cart? cart, Checkout checkout, CancellationToken ct)
    {
        if (cart is not null)
            EnsureOpen(cart);
        if (cart is null || cart.Lines.Count == 0)
            throw new CommandFailedException("cart is empty");

        cart.Status = CartStatus.Ordered;
        cart.OrderNumber = checkout.OrderNumber;
        cart.CustomerName = checkout.Name;
        cart.Email = checkout.Email;
        cart.Address = checkout.Address;
        cart.OrderedUtc = DateTime.UtcNow;
        cart.Total = cart.Lines.Sum(l => l.UnitPrice * l.Quantity);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Cart operation {OperationId} on cart {CartId} hit a database update error", checkout.OperationId, checkout.CartId);
            throw new CommandFailedException("order number collision");
        }
    }

    private static Data.Cart Require(Data.Cart? cart)
    {
        if (cart is null)
            throw new CommandFailedException("cart not found");
        EnsureOpen(cart);
        return cart;
    }

    private static void EnsureOpen(Data.Cart cart)
    {
        if (cart.Status == CartStatus.Ordered)
            throw new CommandFailedException("cart already ordered");
    }
}

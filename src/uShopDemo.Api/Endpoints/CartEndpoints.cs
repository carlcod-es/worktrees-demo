using Microsoft.EntityFrameworkCore;
using uShopDemo.Api.Carts;
using uShopDemo.Api.Data;
using uShopDemo.Api.Sessions;

namespace uShopDemo.Api.Endpoints;

public static class CartEndpoints
{
    /// <summary>Maps cart, operation and order endpoints; every one requires a session.</summary>
    public static void MapShopEndpoints(this IEndpointRouteBuilder app)
    {
        var secured = app.MapGroup("").WithMetadata(RequiresSessionMetadata.Instance);

        var carts = secured.MapGroup("/carts").WithTags("Carts");
        carts.MapGet("/{cartId:guid}", GetCart)
            .WithName("GetCart").WithSummary("Get the cart (empty if unknown or not owned by this session)")
            .Produces<CartResponse>().Produces(StatusCodes.Status401Unauthorized);

        // Caller-supplied product data - demo only: a real shop would look up name/SKU/price server-side.
        carts.MapPost("/{cartId:guid}/lines", AddLine).AddEndpointFilter<JsonBodyFilter>().WithAnyContentType()
            .WithName("AddCartLine").WithSummary("Add a line to the cart (queued)")
            .Produces<OperationAccepted>(StatusCodes.Status202Accepted).ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status415UnsupportedMediaType);
        carts.MapPut("/{cartId:guid}/lines/{productKey:guid}", UpdateLine).AddEndpointFilter<JsonBodyFilter>().WithAnyContentType()
            .WithName("SetCartLineQuantity").WithSummary("Set a line quantity; 0 removes it (queued)")
            .Produces<OperationAccepted>(StatusCodes.Status202Accepted).ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status415UnsupportedMediaType);
        carts.MapDelete("/{cartId:guid}/lines/{productKey:guid}", RemoveLine)
            .WithName("RemoveCartLine").WithSummary("Remove a line from the cart (queued)")
            .Produces<OperationAccepted>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status404NotFound);
        carts.MapPost("/{cartId:guid}/checkout", CheckoutCart).AddEndpointFilter<JsonBodyFilter>().WithAnyContentType()
            .WithName("Checkout").WithSummary("Check out the cart; returns the order number at once (queued)")
            .Produces<CheckoutAccepted>(StatusCodes.Status202Accepted).ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status415UnsupportedMediaType).ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        secured.MapGet("/operations/{operationId:guid}", GetOperation).WithTags("Operations")
            .WithName("GetOperation").WithSummary("Get the status of a queued operation")
            .Produces<OperationResponse>().Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status404NotFound);
        secured.MapGet("/orders/{orderNumber}", GetOrder).WithTags("Orders")
            .WithName("GetOrder").WithSummary("Get an order: pending, confirmed or rejected")
            .Produces<OrderResponse>().Produces(StatusCodes.Status401Unauthorized).Produces(StatusCodes.Status404NotFound);
    }

    private static CartLineResponse ToResponse(CartLine l) =>
        new(l.ProductKey, l.ProductName, l.Sku, l.UnitPrice, l.Quantity, l.UnitPrice * l.Quantity);

    private static async Task<IResult> GetCart(
        Guid cartId, SessionContext session, CartDbContext db, CancellationToken ct)
    {
        var sessionId = session.Id;
        var cart = await db.Carts.AsNoTracking().Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == cartId && c.SessionId == sessionId && c.Status == CartStatus.Open, ct);

        var lines = (cart?.Lines ?? []).OrderBy(l => l.Id).Select(ToResponse).ToList();
        return Results.Ok(new CartResponse(cartId, lines, lines.Sum(l => l.Quantity), lines.Sum(l => l.LineTotal)));
    }

    /// <summary>True if the cart exists and belongs to another session.</summary>
    private static Task<bool> IsForeignAsync(CartDbContext db, Guid cartId, Guid sessionId, CancellationToken ct) =>
        db.Carts.AsNoTracking().AnyAsync(c => c.Id == cartId && c.SessionId != sessionId, ct);

    private static IResult Accepted(Guid operationId, object body) =>
        Results.Accepted($"/operations/{operationId}", body);

    private static async Task<IResult> AddLine(
        Guid cartId, AddLineRequest request, SessionContext session, CartDbContext db, CartQueue queue, CancellationToken ct)
    {
        var errors = Validation.Add(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);
        if (await IsForeignAsync(db, cartId, session.Id, ct))
            return Results.NotFound();

        var operationId = Guid.NewGuid();
        await queue.EnqueueAsync(new AddLine(operationId, cartId, session.Id,
            request.ProductKey!.Value, request.Name!, request.Sku!, request.UnitPrice!.Value, request.Quantity!.Value), ct);
        return Accepted(operationId, new OperationAccepted(operationId));
    }

    private static async Task<IResult> UpdateLine(
        Guid cartId, Guid productKey, UpdateLineRequest request, SessionContext session, CartDbContext db, CartQueue queue, CancellationToken ct)
    {
        var errors = Validation.Update(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);
        if (await IsForeignAsync(db, cartId, session.Id, ct))
            return Results.NotFound();

        var operationId = Guid.NewGuid();
        await queue.EnqueueAsync(new SetQuantity(operationId, cartId, session.Id, productKey, request.Quantity!.Value), ct);
        return Accepted(operationId, new OperationAccepted(operationId));
    }

    private static async Task<IResult> RemoveLine(
        Guid cartId, Guid productKey, SessionContext session, CartDbContext db, CartQueue queue, CancellationToken ct)
    {
        if (await IsForeignAsync(db, cartId, session.Id, ct))
            return Results.NotFound();

        var operationId = Guid.NewGuid();
        await queue.EnqueueAsync(new RemoveLine(operationId, cartId, session.Id, productKey), ct);
        return Accepted(operationId, new OperationAccepted(operationId));
    }

    private static async Task<IResult> CheckoutCart(
        Guid cartId, CheckoutRequest request, SessionContext session, CartDbContext db,
        OperationTracker tracker, CartQueue queue, CancellationToken ct)
    {
        var errors = Validation.Checkout(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);
        if (await IsForeignAsync(db, cartId, session.Id, ct))
            return Results.NotFound();

        var orderNumber = await OrderNumbers.GenerateAsync(db, tracker, ct);
        if (orderNumber is null)
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Could not allocate an order number.");

        // The card number is validated for presence only: never stored, queued, logged or echoed.
        var operationId = Guid.NewGuid();
        await queue.EnqueueAsync(new Checkout(operationId, cartId, session.Id,
            orderNumber, request.Name!, request.Email!, request.Address!), ct);
        return Accepted(operationId, new CheckoutAccepted(operationId, orderNumber));
    }

    private static IResult GetOperation(Guid operationId, SessionContext session, OperationTracker tracker) =>
        tracker.Get(operationId) is { } op && op.SessionId == session.Id
            ? Results.Ok(new OperationResponse(op.OperationId, op.Status, op.Error))
            : Results.NotFound();

    private static async Task<IResult> GetOrder(
        string orderNumber, SessionContext session, CartDbContext db, OperationTracker tracker, CancellationToken ct)
    {
        var sessionId = session.Id;
        var cart = await db.Carts.AsNoTracking().Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.OrderNumber == orderNumber && c.Status == CartStatus.Ordered && c.SessionId == sessionId, ct);

        if (cart is not null)
        {
            var lines = cart.Lines.OrderBy(l => l.Id).Select(ToResponse).ToList();
            return Results.Ok(new OrderResponse(
                orderNumber, "confirmed",
                Name: cart.CustomerName, Email: cart.Email, Address: cart.Address,
                OrderedUtc: cart.OrderedUtc is { } ordered ? DateTime.SpecifyKind(ordered, DateTimeKind.Utc) : null,
                Lines: lines, ItemCount: lines.Sum(l => l.Quantity), Total: cart.Total ?? lines.Sum(l => l.LineTotal)));
        }

        if (tracker.GetByOrderNumber(orderNumber) is { } op && op.SessionId == sessionId)
        {
            switch (op.Status)
            {
                case OperationStatus.Queued or OperationStatus.Completed:
                    return Results.Ok(new OrderResponse(orderNumber, "pending"));
                case OperationStatus.Failed:
                    return Results.Ok(new OrderResponse(orderNumber, "rejected", Reason: op.Error));
            }
        }

        return Results.NotFound();
    }
}

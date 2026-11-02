using System.Threading.Channels;

namespace uShopDemo.Api.Carts;

/// <summary>Records the operation as queued, then hands the command to the worker.</summary>
public sealed class CartQueue(Channel<CartCommand> channel, OperationTracker tracker)
{
    public static Channel<CartCommand> CreateChannel() =>
        Channel.CreateBounded<CartCommand>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        });

    public async ValueTask EnqueueAsync(CartCommand command, CancellationToken cancellationToken = default)
    {
        tracker.MarkQueued(command.OperationId, command.SessionId, (command as Checkout)?.OrderNumber);
        try
        {
            await channel.Writer.WriteAsync(command, cancellationToken);
        }
        catch
        {
            tracker.MarkFailed(command.OperationId, "could not queue operation");
            throw;
        }
    }
}

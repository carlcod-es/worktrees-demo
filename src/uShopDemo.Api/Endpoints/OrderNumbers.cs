using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using uShopDemo.Api.Carts;
using uShopDemo.Api.Data;

namespace uShopDemo.Api.Endpoints;

internal static class OrderNumbers
{
    private const int MaxAttempts = 10;

    /// <summary>US-yyMMdd-NNNNN, unused in both the tracker and the database. Null if every attempt collided.</summary>
    public static async Task<string?> GenerateAsync(CartDbContext db, OperationTracker tracker, CancellationToken ct)
    {
        for (var i = 0; i < MaxAttempts; i++)
        {
            var number = $"US-{DateTime.UtcNow:yyMMdd}-{RandomNumberGenerator.GetInt32(100_000):D5}";
            if (tracker.GetByOrderNumber(number) is null
                && !await db.Carts.AnyAsync(c => c.OrderNumber == number, ct))
                return number;
        }
        return null;
    }
}

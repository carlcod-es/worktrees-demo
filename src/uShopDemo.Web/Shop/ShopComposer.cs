using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using uShopDemo.Web.Shop.Data;
using uShopDemo.Web.Shop.Seeding;

namespace uShopDemo.Web.Shop;

public class ShopComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Shares Umbraco's connection string and provider (SQL Server under Aspire).
        builder.Services.AddUmbracoDbContext<ShopDbContext>((sp, options, _, _) => options.UseUmbracoDatabaseProvider(sp), shareUmbracoConnection: true);
        builder.Services.AddScoped<ICartService, CartService>();
        builder.Services.AddTransient<CatalogueSeeder>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, ShopStartup>();
    }
}

/// <summary>Runs after Umbraco is installed and before requests are served: apply EF Core migrations, then seed.</summary>
public class ShopStartup(
    IRuntimeState runtimeState,
    IServiceScopeFactory scopeFactory,
    ILogger<ShopStartup> logger) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (runtimeState.Level != RuntimeLevel.Run)
        {
            logger.LogWarning("Umbraco runtime level is {Level}; skipping shop migrations and seeding", runtimeState.Level);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Applying shop migrations: {Migrations}", string.Join(", ", pending));
            await db.Database.MigrateAsync(cancellationToken);
        }

        await scope.ServiceProvider.GetRequiredService<CatalogueSeeder>().SeedAsync();
    }
}

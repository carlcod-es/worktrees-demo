using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace uShopDemo.Api.Data;

public class CartDbContext(DbContextOptions<CartDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public const string MigrationsHistoryTable = "__ushopMigrationsHistory";

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartLine> CartLines => Set<CartLine>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <summary>Shared by runtime registration and the design-time factory so the history table cannot drift.</summary>
    public static void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql) =>
        sql.MigrationsHistoryTable(MigrationsHistoryTable);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(e =>
        {
            e.ToTable("ushopCart");
            e.HasKey(x => x.Id);
            e.Property(x => x.SessionId).IsRequired();
            e.HasIndex(x => x.SessionId);
            e.Property(x => x.OrderNumber).HasMaxLength(32);
            e.HasIndex(x => x.OrderNumber).IsUnique().HasFilter("[OrderNumber] IS NOT NULL");
            e.Property(x => x.CustomerName).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.Address).HasMaxLength(1000);
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartLine>(e =>
        {
            e.ToTable("ushopCartLine");
            e.Property(x => x.ProductName).HasMaxLength(200);
            e.Property(x => x.Sku).HasMaxLength(64);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CartId, x.ProductKey }).IsUnique();
        });

        modelBuilder.Entity<DataProtectionKey>().ToTable("ushopDataProtectionKeys");
    }
}

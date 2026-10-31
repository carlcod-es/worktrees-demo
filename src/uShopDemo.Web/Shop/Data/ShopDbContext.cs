using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace uShopDemo.Web.Shop.Data;

public enum CartStatus
{
    Open = 0,
    Ordered = 1,
}

/// <summary>A visitor's cart. Becomes an order when <see cref="Status"/> is Ordered. <see cref="Id"/> is the cookie value.</summary>
public class Cart
{
    public Guid Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public CartStatus Status { get; set; }
    public string? OrderNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime? OrderedUtc { get; set; }
    public decimal? Total { get; set; }
    public List<CartLine> Lines { get; set; } = [];
}

/// <summary>Name, SKU and unit price are captured when the line is added.</summary>
public class CartLine
{
    public int Id { get; set; }
    public Guid CartId { get; set; }
    public Guid ProductKey { get; set; }
    public required string ProductName { get; set; }
    public required string Sku { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartLine> CartLines => Set<CartLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(e =>
        {
            e.ToTable("ushopCart");
            e.HasKey(x => x.Id);
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
        });
    }
}

/// <summary>Lets <c>dotnet ef migrations add</c> run without a database or a booted Umbraco.</summary>
public class ShopDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ShopDbContext>
{
    public ShopDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ShopDbContext>()
            .UseSqlServer("Server=localhost;Database=design-time;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);
}

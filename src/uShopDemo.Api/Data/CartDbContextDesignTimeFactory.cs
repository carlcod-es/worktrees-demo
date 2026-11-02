using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace uShopDemo.Api.Data;

/// <summary>Lets <c>dotnet ef migrations add</c> run without a database or the host.</summary>
public class CartDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CartDbContext>
{
    public CartDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CartDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=design-time;Trusted_Connection=True;TrustServerCertificate=True",
                CartDbContext.ConfigureSqlServer)
            .Options);
}

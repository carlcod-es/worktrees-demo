using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using uShopDemo.Api.Carts;
using uShopDemo.Api.Data;
using uShopDemo.Api.Endpoints;
using uShopDemo.Api.Sessions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<CartDbContext>(
    "umbracoDbDSN",
    configureDbContextOptions: options => options.UseSqlServer(CartDbContext.ConfigureSqlServer));

builder.Services.AddHealthChecks();
builder.Services.AddApiDocument();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddDataProtection()
    .SetApplicationName("uShopDemo.Api")
    .PersistKeysToDbContext<CartDbContext>();
builder.Services.AddSingleton<SessionCookie>();
builder.Services.AddScoped<SessionContext>();

// Exactly the site origin; no origin configured means no cross-origin access.
var webOrigin = WebOrigin.Read(builder.Configuration);
builder.Services.AddCors(o => o.AddDefaultPolicy(policy =>
{
    if (webOrigin is not null)
        policy.WithOrigins(webOrigin);
    policy.WithMethods("GET", "POST", "PUT", "DELETE").WithHeaders("Content-Type").WithExposedHeaders("Location").AllowCredentials();
}));

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(CartQueue.CreateChannel());
builder.Services.AddSingleton<OperationTracker>();
builder.Services.AddSingleton<CartQueue>();
builder.Services.AddHostedService<CartWorker>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();
    if (db.Database.IsSqlServer())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();
}

app.UseRouting();
app.UseCors();
app.UseMiddleware<SessionMiddleware>();

app.MapSessionEndpoints();
app.MapShopEndpoints();
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await app.RunAsync();

public partial class Program;

var builder = DistributedApplication.CreateBuilder(args);

// No volume, no persistent lifetime: every AppHost run gets its own SQL Server container
// (random name suffix, dynamic host port) and an empty database.
var sql = builder.AddSqlServer("sql");
var db = sql.AddDatabase("umbracoDbDSN");

// Fixed web port supplied by scripts/dev.mjs; null = dynamic.
int? webPort = int.TryParse(builder.Configuration["BLOG_WEB_PORT"], out var p) ? p : null;

builder.AddProject<Projects.Blog_Web>("web")
    .WithReference(db)
    .WaitFor(db)
    .WithHttpEndpoint(port: webPort, name: "http")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ConnectionStrings__umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
    .WithEnvironment("BLOG_BRANCH", builder.Configuration["BLOG_BRANCH"] ?? "unknown");

builder.Build().Run();

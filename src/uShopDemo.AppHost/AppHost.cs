using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);

// No volume, no persistent lifetime: every AppHost run gets its own SQL Server container
// (random name suffix, dynamic host port) and an empty database.
var sql = builder.AddSqlServer("sql");
var db = sql.AddDatabase("umbracoDbDSN");

// Fixed web port supplied by scripts/dev.mjs; null = dynamic (plain `aspire run`).
int? webPort = int.TryParse(builder.Configuration["USHOP_WEB_PORT"], out var p) ? p : null;

int? apiPort = int.TryParse(builder.Configuration["USHOP_API_PORT"], out var ap) ? ap : null;

// Branch from scripts/dev.mjs, else ask git so a plain `aspire run` still shows the badge.
var branch = builder.Configuration["USHOP_BRANCH"] ?? CurrentGitBranch(builder.AppHostDirectory) ?? "unknown";

// Cart API shares the Umbraco database (own ushop* tables, own migrations history). The browser calls it directly.
var api = builder.AddProject<Projects.uShopDemo_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WithHttpsEndpoint(port: apiPort, name: "https")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health", endpointName: "https");

var web = builder.AddProject<Projects.uShopDemo_Web>("web")
    .WithReference(db)
    .WaitFor(db)
    // Web starts only once the API health check passes. Endpoint references create no cycle with the api -> web origin below.
    .WaitFor(api)
    // HTTPS only: the backoffice login needs it. Kestrel serves the ASP.NET dev cert (CN=localhost, any port),
    // so every worktree can have its own port. One scheme avoids http/https redirect-URI mismatches in the
    // backoffice OpenID client.
    .WithHttpsEndpoint(port: webPort, name: "https")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ConnectionStrings__umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
    .WithEnvironment("USHOP_BRANCH", branch)
    .WithEnvironment("USHOP_API_URL", api.GetEndpoint("https"));

api.WithEnvironment("USHOP_WEB_ORIGIN", web.GetEndpoint("https"));

builder.Build().Run();

static string? CurrentGitBranch(string dir)
{
    try
    {
        using var git = Process.Start(new ProcessStartInfo("git", ["symbolic-ref", "--short", "HEAD"])
        {
            WorkingDirectory = dir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        if (git is null) return null;
        var output = git.StandardOutput.ReadToEnd().Trim();
        git.WaitForExit();
        return git.ExitCode == 0 && output.Length > 0 ? output : null;
    }
    catch
    {
        return null; // git not installed
    }
}

using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);

// No volume, no persistent lifetime: every AppHost run gets its own SQL Server container
// (random name suffix, dynamic host port) and an empty database.
var sql = builder.AddSqlServer("sql");
var db = sql.AddDatabase("umbracoDbDSN");

// Fixed web port supplied by scripts/dev.mjs; null = dynamic (plain `aspire run`).
int? webPort = int.TryParse(builder.Configuration["USHOP_WEB_PORT"], out var p) ? p : null;

// Branch from scripts/dev.mjs, else ask git so a plain `aspire run` still shows the badge.
var branch = builder.Configuration["USHOP_BRANCH"] ?? CurrentGitBranch(builder.AppHostDirectory) ?? "unknown";

builder.AddProject<Projects.uShopDemo_Web>("web")
    .WithReference(db)
    .WaitFor(db)
    // HTTPS only: the backoffice login needs it. Kestrel serves the ASP.NET dev cert (CN=localhost, any port),
    // so every worktree can have its own port. One scheme avoids http/https redirect-URI mismatches in the
    // backoffice OpenID client.
    .WithHttpsEndpoint(port: webPort, name: "https")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ConnectionStrings__umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
    .WithEnvironment("USHOP_BRANCH", branch);

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

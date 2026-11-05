WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ADR-0034: telemetry. The OTLP log sink is picked up by Umbraco's Serilog pipeline.
builder.AddServiceDefaults();
builder.AddSerilogOtlpSink();

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

// Friendly pages apply to site paths only; the backoffice keeps Umbraco's own error handling.
// In Development the auto-added developer exception page sits at the very start of the pipeline, outside these
// branches, so the handler below catches site exceptions first and the developer page only sees /umbraco ones.
static bool IsSitePath(HttpContext context) => !context.Request.Path.StartsWithSegments("/umbraco");

// Friendly 500 in every environment. Serves the static page directly with a hard 500 status
// (re-executing a static file path would return 200) and cannot loop if the file is missing.
app.UseWhen(IsSitePath, branch => branch.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    var file = app.Environment.WebRootFileProvider.GetFileInfo("errors/500.html");
    if (file.Exists)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(file);
    }
})));

// Friendly 4xx for empty responses (the middleware skips responses that already have a body or content type).
// Serves the file directly with the original status; re-executing a static path would turn it into 200.
// Only 400-499 get the page; empty 5xx responses are left untouched (unhandled exceptions get the 500 page above).
app.UseWhen(IsSitePath, branch => branch.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (response.StatusCode is < 400 or > 499) return;
    var file = app.Environment.WebRootFileProvider.GetFileInfo("errors/4xx.html");
    if (file.Exists)
    {
        response.ContentType = "text/html; charset=utf-8";
        await response.SendFileAsync(file);
    }
}));

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();

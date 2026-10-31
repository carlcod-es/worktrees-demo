using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace Blog.Web.Seeding;

public class BlogSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, BlogSeeder>();
}

/// <summary>
/// Idempotent: creates the blog content model, templates and sample posts if no "blog" root exists.
/// Presentation lives only in Views/ and wwwroot/; this class never changes between layout branches.
/// </summary>
public class BlogSeeder(
    IContentTypeService contentTypes,
    IContentService content,
    IDataTypeService dataTypes,
    ITemplateService templates,
    IShortStringHelper shortString,
    IWebHostEnvironment env,
    ILogger<BlogSeeder> logger) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static readonly Guid UserKey = Constants.Security.SuperUserKey;

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (content.GetRootContent().Any(c => c.ContentType.Alias == "blog"))
        {
            return;
        }

        logger.LogInformation("Seeding blog content");

        var text = await dataTypes.GetAsync(Constants.DataTypes.Guids.TextstringGuid) ?? throw new InvalidOperationException("Textstring data type missing");
        var area = await dataTypes.GetAsync(Constants.DataTypes.Guids.TextareaGuid) ?? throw new InvalidOperationException("Textarea data type missing");
        var date = await dataTypes.GetAsync(Constants.DataTypes.Guids.DatePickerWithTimeGuid) ?? throw new InvalidOperationException("Datetime data type missing");

        var blogTemplate = await CreateTemplateAsync("Blog", "blog");
        var postTemplate = await CreateTemplateAsync("Blog Post", "blogPost");

        var post = new ContentType(shortString, -1) { Alias = "blogPost", Name = "Blog Post", Icon = "icon-article", AllowedAsRoot = false };
        AddProp(post, text, "title", "Title");
        AddProp(post, date, "published", "Published");
        AddProp(post, text, "author", "Author");
        AddProp(post, area, "summary", "Summary");
        AddProp(post, area, "body", "Body (HTML)");
        AddProp(post, text, "image", "Image path");
        post.AllowedTemplates = [postTemplate];
        post.SetDefaultTemplate(postTemplate);
        Check(await contentTypes.CreateAsync(post, UserKey), "create blogPost type");

        var blog = new ContentType(shortString, -1) { Alias = "blog", Name = "Blog", Icon = "icon-home", AllowedAsRoot = true };
        AddProp(blog, text, "siteTitle", "Site title");
        AddProp(blog, text, "tagline", "Tagline");
        blog.AllowedTemplates = [blogTemplate];
        blog.SetDefaultTemplate(blogTemplate);
        blog.AllowedContentTypes = [new ContentTypeSort(post.Key, 0, post.Alias)];
        Check(await contentTypes.CreateAsync(blog, UserKey), "create blog type");

        var root = content.Create("Blog", Constants.System.Root, "blog");
        root.TemplateId = blogTemplate.Id;
        root.SetValue("siteTitle", "Worktree Notes");
        root.SetValue("tagline", "Field notes on running many things at once");
        Check(content.Save(root), "save blog");
        Check(content.Publish(root, ["*"]), "publish blog");

        var start = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < Posts.Length; i++)
        {
            var (title, author, summary, body) = Posts[i];
            var p = content.Create(title, root, "blogPost");
            p.TemplateId = postTemplate.Id;
            p.SetValue("title", title);
            p.SetValue("published", start.AddDays(i * 6));
            p.SetValue("author", author);
            p.SetValue("summary", summary);
            p.SetValue("body", body);
            p.SetValue("image", $"/img/post{i + 1}.svg");
            Check(content.Save(p), $"save {title}");
            Check(content.Publish(p, ["*"]), $"publish {title}");
        }
    }

    private async Task<ITemplate> CreateTemplateAsync(string name, string alias)
    {
        // Use the on-disk view so Umbraco's template record and the file stay identical.
        var path = Path.Combine(env.ContentRootPath, "Views", alias + ".cshtml");
        var markup = System.IO.File.Exists(path) ? await System.IO.File.ReadAllTextAsync(path) : null;
        var result = await templates.CreateAsync(name, alias, markup, UserKey);
        if (!result.Success || result.Result is null)
        {
            throw new InvalidOperationException($"Template {alias}: {result.Status}");
        }
        return result.Result;
    }

    private void AddProp(ContentType type, IDataType dt, string alias, string name)
    {
        var group = type.PropertyGroups.FirstOrDefault() ?? new PropertyGroup(true) { Alias = "content", Name = "Content", Type = PropertyGroupType.Group };
        group.PropertyTypes!.Add(new PropertyType(shortString, dt, alias) { Name = name });
        if (!type.PropertyGroups.Contains(group.Alias)) type.PropertyGroups.Add(group);
    }

    private static void Check(Attempt<ContentTypeOperationStatus> a, string what) { if (!a.Success) throw new InvalidOperationException($"{what}: {a.Result}"); }
    private static void Check(OperationResult r, string what) { if (!r.Success) throw new InvalidOperationException($"{what}: {r.Result}"); }
    private static void Check(PublishResult r, string what) { if (!r.Success) throw new InvalidOperationException($"{what}: {r.Result}"); }

    private static readonly (string Title, string Author, string Summary, string Body)[] Posts =
    [
        ("One repo, three checkouts", "Carl", "A worktree is a second working directory on the same repository.",
            "<p>A worktree shares the object database with your main checkout but has its own HEAD, index and files.</p><p>That means three branches can be checked out at once, with no stashing and no cloning.</p><p>Disk cost is the working files only.</p>"),
        ("Why agents want their own directory", "Carl", "Parallel agents trip over each other in a shared folder.",
            "<p>Two agents editing one directory race on files, builds and ports.</p><p>Give each agent a worktree and a branch and the conflicts move to merge time, where git is good at them.</p>"),
        ("Ports are the real collision", "Carl", "Files are easy to isolate. Listening sockets are not.",
            "<p>Three copies of the same site all want port 5000.</p><p>Derive ports from the worktree path and every checkout gets a stable, distinct address.</p>"),
        ("Databases per branch", "Carl", "A throwaway SQL Server per worktree keeps experiments honest.",
            "<p>Aspire starts a SQL Server container per AppHost with no volume.</p><p>Each run starts empty and reseeds, so a layout experiment can never corrupt another one.</p>"),
        ("Comparing layouts side by side", "Carl", "The payoff: three tabs, three designs, one content set.",
            "<p>Same posts, same model, different views and CSS.</p><p>Putting them next to each other beats flipping branches and reloading.</p>"),
        ("Cleaning up", "Carl", "Remove the worktrees, keep the branches.",
            "<p><code>git worktree remove</code> deletes the directory and leaves the branch for merging.</p>")
    ];
}

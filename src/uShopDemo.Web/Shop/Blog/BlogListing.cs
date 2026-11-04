using System.Globalization;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace uShopDemo.Web.Shop.Blog;

/// <summary>One page of blog articles plus paging state (ADR-0029).</summary>
public sealed record BlogPage(IReadOnlyList<IPublishedContent> Articles, int CurrentPage, int TotalPages);

/// <summary>Server-side blog listing: newest first, five per page, forgiving page parsing (ADR-0029).</summary>
public static class BlogListing
{
    public const int PageSize = 5;

    private const string ArticleAlias = "blogArticle";

    /// <summary>Published <c>blogArticle</c> children, by publishDate descending then title ascending, for the clamped page.</summary>
    public static BlogPage ForPage(IPublishedContent blog, string? rawPage)
    {
        var articles = blog.Children()!
            .Where(c => c.ContentType.Alias == ArticleAlias)
            .OrderByDescending(c => c.Value<DateTime>("publishDate"))
            .ThenBy(c => c.Value<string>("title") ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var (page, totalPages, skip) = Clamp(articles.Count, rawPage, PageSize);
        return new BlogPage(articles.Skip(skip).Take(PageSize).ToList(), page, totalPages);
    }

    /// <summary>
    /// Pure paging maths. Missing, empty, non-integer, int-overflowing, zero or negative input gives page 1;
    /// a whole number past the end gives the last page. Zero items gives page 1 of 1.
    /// </summary>
    public static (int Page, int TotalPages, int Skip) Clamp(int totalItems, string? rawPage, int pageSize = PageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var totalPages = totalItems <= 0 ? 1 : totalItems / pageSize + (totalItems % pageSize > 0 ? 1 : 0);

        // NumberStyles.None rejects signs, whitespace and separators; overflow fails TryParse.
        var page = int.TryParse(rawPage, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed >= 1
            ? Math.Min(parsed, totalPages)
            : 1;

        return (page, totalPages, (page - 1) * pageSize);
    }
}

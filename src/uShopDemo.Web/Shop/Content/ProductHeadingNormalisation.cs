using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace uShopDemo.Web.Shop.Content;

/// <summary>Normalises heading levels in rich-text blocks in product descriptions and blog article main content on save (ADR-0025).</summary>
public sealed class ProductHeadingNormalisationHandler : INotificationHandler<ContentSavingNotification>
{
    private static readonly Dictionary<string, string> PropertyAliasByContentType = new()
    {
        ["product"] = "description",
        ["blogArticle"] = "mainContent",
    };

    public void Handle(ContentSavingNotification notification)
    {
        foreach (var content in notification.SavedEntities)
        {
            if (!PropertyAliasByContentType.TryGetValue(content.ContentType.Alias, out var propertyAlias)
                || !content.HasProperty(propertyAlias))
            {
                continue;
            }

            if (content.GetValue<string>(propertyAlias) is { } json
                && HeadingNormaliser.NormaliseBlockListJson(json) is { } normalised)
            {
                content.SetValue(propertyAlias, normalised);
            }
        }
    }
}

public sealed class ProductHeadingNormalisationComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.AddNotificationHandler<ContentSavingNotification, ProductHeadingNormalisationHandler>();
}

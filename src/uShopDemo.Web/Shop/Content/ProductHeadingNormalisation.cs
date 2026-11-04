using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace uShopDemo.Web.Shop.Content;

/// <summary>Normalises heading levels in product description rich-text blocks on save (ADR-0025).</summary>
public sealed class ProductHeadingNormalisationHandler : INotificationHandler<ContentSavingNotification>
{
    private const string ProductAlias = "product";
    private const string DescriptionAlias = "description";

    public void Handle(ContentSavingNotification notification)
    {
        foreach (var content in notification.SavedEntities)
        {
            if (content.ContentType.Alias != ProductAlias || !content.HasProperty(DescriptionAlias))
            {
                continue;
            }

            if (content.GetValue<string>(DescriptionAlias) is { } json
                && HeadingNormaliser.NormaliseBlockListJson(json) is { } normalised)
            {
                content.SetValue(DescriptionAlias, normalised);
            }
        }
    }
}

public sealed class ProductHeadingNormalisationComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) =>
        builder.AddNotificationHandler<ContentSavingNotification, ProductHeadingNormalisationHandler>();
}

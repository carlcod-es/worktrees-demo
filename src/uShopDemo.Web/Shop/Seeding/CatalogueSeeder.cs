using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Strings;

namespace uShopDemo.Web.Shop.Seeding;

/// <summary>
/// Idempotent: creates the shop content model, templates and catalogue if no "shop" root exists.
/// Presentation lives only in Views/ and wwwroot/; this class never changes between layout branches.
/// </summary>
public class CatalogueSeeder(
    IContentTypeService contentTypes,
    IContentService content,
    IDataTypeService dataTypes,
    ITemplateService templates,
    PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer configSerializer,
    IShortStringHelper shortString,
    IWebHostEnvironment env,
    ILogger<CatalogueSeeder> logger)
{
    private static readonly Guid UserKey = Constants.Security.SuperUserKey;

    public async Task SeedAsync()
    {
        if (content.GetRootContent().Any(c => c.ContentType.Alias == "shop"))
        {
            return;
        }

        logger.LogInformation("Seeding shop catalogue");

        var text = await dataTypes.GetAsync(Constants.DataTypes.Guids.TextstringGuid) ?? throw new InvalidOperationException("Textstring data type missing");
        var area = await dataTypes.GetAsync(Constants.DataTypes.Guids.TextareaGuid) ?? throw new InvalidOperationException("Textarea data type missing");
        var price = await GetOrCreatePriceDataTypeAsync();

        var shopTemplate = await CreateTemplateAsync("Shop", "shop");
        var categoryTemplate = await CreateTemplateAsync("Category", "category");
        var productTemplate = await CreateTemplateAsync("Product", "product");
        var cartTemplate = await CreateTemplateAsync("Cart", "cart");
        var checkoutTemplate = await CreateTemplateAsync("Checkout", "checkout");
        var confirmationTemplate = await CreateTemplateAsync("Order Confirmation", "orderConfirmation");

        var product = await CreateTypeAsync("product", "Product", "icon-box", productTemplate, [],
            (text, "title", "Title"), (text, "sku", "SKU"), (price, "price", "Price"),
            (area, "summary", "Summary"), (area, "description", "Description (HTML)"), (text, "image", "Image path"));
        var category = await CreateTypeAsync("category", "Category", "icon-folder", categoryTemplate, [product],
            (text, "title", "Title"), (area, "intro", "Intro"), (text, "image", "Image path"));
        var cart = await CreateTypeAsync("cart", "Cart", "icon-shopping-basket", cartTemplate, [], (text, "title", "Title"));
        var checkout = await CreateTypeAsync("checkout", "Checkout", "icon-credit-card", checkoutTemplate, [], (text, "title", "Title"));
        var confirmation = await CreateTypeAsync("orderConfirmation", "Order Confirmation", "icon-check", confirmationTemplate, [], (text, "title", "Title"));
        await CreateTypeAsync("shop", "Shop", "icon-store", shopTemplate, [category, cart, checkout, confirmation],
            (text, "siteTitle", "Site title"), (text, "tagline", "Tagline"));

        var root = Publish(content.Create("uShop", Constants.System.Root, "shop"), shopTemplate, new()
        {
            ["siteTitle"] = "uShop",
            ["tagline"] = "Small-batch coffee and the gear to brew it",
        });

        foreach (var c in Catalogue)
        {
            var cat = Publish(content.Create(c.Name, root, "category"), categoryTemplate, new()
            {
                ["title"] = c.Name,
                ["intro"] = c.Intro,
                ["image"] = $"/img/categories/{c.Slug}.svg",
            });
            foreach (var p in c.Products)
            {
                Publish(content.Create(p.Name, cat, "product"), productTemplate, new()
                {
                    ["title"] = p.Name,
                    ["sku"] = p.Sku,
                    ["price"] = p.Price,
                    ["summary"] = p.Summary,
                    ["description"] = p.Description,
                    ["image"] = $"/img/products/{p.Sku.ToLowerInvariant()}.svg",
                });
            }
        }

        Publish(content.Create("Cart", root, "cart"), cartTemplate, new() { ["title"] = "Your cart" });
        Publish(content.Create("Checkout", root, "checkout"), checkoutTemplate, new() { ["title"] = "Checkout" });
        Publish(content.Create("Order confirmation", root, "orderConfirmation"), confirmationTemplate, new() { ["title"] = "Thank you" });
    }

    private IContent Publish(IContent node, ITemplate template, Dictionary<string, object?> values)
    {
        node.TemplateId = template.Id;
        foreach (var (alias, value) in values) node.SetValue(alias, value);
        Check(content.Save(node), $"save {node.Name}");
        Check(content.Publish(node, ["*"]), $"publish {node.Name}");
        return node;
    }

    private async Task<IDataType> GetOrCreatePriceDataTypeAsync()
    {
        const string name = "Price";
        if (await dataTypes.GetAsync(name) is { } existing) return existing;

        if (!propertyEditors.TryGet(Constants.PropertyEditors.Aliases.Decimal, out var editor))
            throw new InvalidOperationException("Decimal property editor missing");
        var dt = new DataType(editor, configSerializer)
        {
            Name = name,
            EditorUiAlias = "Umb.PropertyEditorUi.Decimal",
            DatabaseType = ValueStorageType.Decimal,
        };
        var result = await dataTypes.CreateAsync(dt, UserKey);
        if (!result.Success) throw new InvalidOperationException($"create Price data type: {result.Status}");
        return result.Result;
    }

    private async Task<ContentType> CreateTypeAsync(string alias, string name, string icon, ITemplate template,
        IContentType[] allowedChildren, params (IDataType Type, string Alias, string Name)[] props)
    {
        var type = new ContentType(shortString, -1) { Alias = alias, Name = name, Icon = icon, AllowedAsRoot = alias == "shop" };
        var group = new PropertyGroup(true) { Alias = "content", Name = "Content", Type = PropertyGroupType.Group };
        foreach (var p in props) group.PropertyTypes!.Add(new PropertyType(shortString, p.Type, p.Alias) { Name = p.Name });
        type.PropertyGroups.Add(group);
        type.AllowedTemplates = [template];
        type.SetDefaultTemplate(template);
        type.AllowedContentTypes = allowedChildren.Select((c, i) => new ContentTypeSort(c.Key, i, c.Alias)).ToArray();
        Check(await contentTypes.CreateAsync(type, UserKey), $"create {alias} type");
        return type;
    }

    private async Task<ITemplate> CreateTemplateAsync(string name, string alias)
    {
        // Existing template (earlier partial seed) is reused.
        if (await templates.GetAsync(alias) is { } existing) return existing;

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

    private static void Check(Attempt<ContentTypeOperationStatus> a, string what) { if (!a.Success) throw new InvalidOperationException($"{what}: {a.Result}"); }
    private static void Check(OperationResult r, string what) { if (!r.Success) throw new InvalidOperationException($"{what}: {r.Result}"); }
    private static void Check(PublishResult r, string what) { if (!r.Success) throw new InvalidOperationException($"{what}: {r.Result}"); }

    private sealed record Product(string Name, string Sku, decimal Price, string Summary, string Description);
    private sealed record Category(string Name, string Slug, string Intro, Product[] Products);

    private static readonly Category[] Catalogue =
    [
        new("Coffee", "coffee", "Whole-bean coffee, roasted weekly in small batches.",
        [
            new("Ethiopia Yirgacheffe", "COF-ETH", 14.50m, "Floral and bright with notes of bergamot and peach.",
                "<p>Washed heirloom varieties grown at 1,900 m.</p><p>Light roast. Best as pour-over.</p><p>250 g whole bean.</p>"),
            new("Colombia Huila", "COF-COL", 12.90m, "Balanced and sweet: caramel, red apple, cocoa.",
                "<p>A crowd-pleaser from smallholder farms in Huila.</p><p>Medium roast. Works in any brewer.</p><p>250 g whole bean.</p>"),
            new("House Espresso", "COF-ESP", 32.00m, "Chocolate-heavy blend built for milk drinks.",
                "<p>Brazil and Guatemala, roasted a touch darker.</p><p>Dense crema, low acidity.</p><p>1 kg whole bean.</p>"),
        ]),
        new("Brewers", "brewers", "Everything you need to turn beans into a cup.",
        [
            new("Pour-over Dripper", "BRW-DRIP", 24.00m, "Ceramic cone dripper for one or two cups.",
                "<p>Spiral ribs and a single large hole give you full control of flow.</p><p>Fits size 02 paper filters.</p>"),
            new("French Press 1 L", "BRW-PRESS", 39.00m, "Borosilicate glass and steel, eight cups.",
                "<p>Four-minute immersion brewing with a fine steel mesh.</p><p>Dishwasher safe.</p>"),
            new("Moka Pot 6-cup", "BRW-MOKA", 34.50m, "Stovetop espresso-style coffee in five minutes.",
                "<p>Cast aluminium, works on gas and electric hobs.</p><p>Induction adapter sold separately.</p>"),
        ]),
        new("Grinders", "grinders", "Fresh grounds make the biggest difference.",
        [
            new("Hand Grinder", "GRD-HAND", 79.00m, "Steel conical burrs, 30 g capacity.",
                "<p>Stepped adjustment from espresso to cold brew.</p><p>Packs small for travel.</p>"),
            new("Electric Burr Grinder", "GRD-BURR", 149.00m, "40 grind settings and a timed dose.",
                "<p>Flat steel burrs, low retention, quiet motor.</p><p>Includes a dosing cup.</p>"),
            new("Blade Grinder", "GRD-BLADE", 29.00m, "Simple, fast and cheap. Also does spices.",
                "<p>Pulse for coarser grounds, hold for finer.</p><p>Removable bowl.</p>"),
        ]),
        new("Accessories", "accessories", "Small tools for repeatable cups.",
        [
            new("Gooseneck Kettle", "ACC-KETTLE", 59.00m, "Precise pour with temperature hold.",
                "<p>0.9 L, adjustable 40-100 °C, keeps warm for an hour.</p><p>Matte black.</p>"),
            new("Brewing Scale", "ACC-SCALE", 27.50m, "0.1 g resolution with a built-in timer.",
                "<p>Weigh beans and water for consistent ratios.</p><p>USB-C rechargeable.</p>"),
            new("Stoneware Mug", "ACC-MUG", 16.00m, "Hand-glazed, 350 ml.",
                "<p>Each one slightly different.</p><p>Microwave and dishwasher safe.</p>"),
        ]),
    ];
}

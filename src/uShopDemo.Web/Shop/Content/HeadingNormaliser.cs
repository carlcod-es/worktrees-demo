using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace uShopDemo.Web.Shop.Content;

/// <summary>
/// Pure functions that clamp rich-text headings to h2/h3 inside Block List rich text (product description, blog article main content) (ADR-0025).
/// </summary>
public static partial class HeadingNormaliser
{
    // Must match the key in uSync/v18/ContentTypes/ElementTypes/richTextBlock.config.
    public static readonly Guid RichTextBlockKey = new("7e5b24ac-fc8e-4b56-93b7-9820810ef7d7");
    public const string TextAlias = "text";

    [GeneratedRegex(@"<h1(\s|>)", RegexOptions.IgnoreCase)]
    private static partial Regex H1Open();

    [GeneratedRegex(@"</h1\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex H1Close();

    [GeneratedRegex(@"<h[456](\s|>)", RegexOptions.IgnoreCase)]
    private static partial Regex DeepOpen();

    [GeneratedRegex(@"</h[456]\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex DeepClose();

    /// <summary>h1 becomes h2; h4-h6 become h3. Attributes and everything else are kept.</summary>
    public static string NormaliseMarkup(string markup)
    {
        var result = H1Open().Replace(markup, "<h2$1");
        result = H1Close().Replace(result, "</h2>");
        result = DeepOpen().Replace(result, "<h3$1");
        return DeepClose().Replace(result, "</h3>");
    }

    /// <summary>
    /// Rewrites the markup of every rich-text block in a Block List value.
    /// Returns null when nothing changed (or the value isn't a usable Block List), so callers leave the value alone.
    /// </summary>
    public static string? NormaliseBlockListJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (root is not JsonObject obj || obj["contentData"] is not JsonArray blocks)
        {
            return null;
        }

        var changed = false;
        foreach (var block in blocks.OfType<JsonObject>())
        {
            if (!IsRichTextBlock(block)
                || block["values"] is not JsonArray values)
            {
                continue;
            }

            foreach (var value in values.OfType<JsonObject>())
            {
                if (value["alias"] is not JsonValue aliasNode || !aliasNode.TryGetValue<string>(out var alias) || alias != TextAlias)
                {
                    continue;
                }

                // The backoffice stores a rich-text value as a JSON string; uSync files can hold it as an object.
                if (value["value"] is JsonObject text)
                {
                    changed |= NormaliseTextObject(text);
                }
                else if (value["value"] is JsonValue raw
                    && raw.TryGetValue<string>(out var inner)
                    && ParseObject(inner) is { } innerText
                    && NormaliseTextObject(innerText))
                {
                    value["value"] = innerText.ToJsonString();
                    changed = true;
                }
            }
        }

        return changed ? obj.ToJsonString() : null;
    }

    private static bool IsRichTextBlock(JsonObject block) =>
        block["contentTypeKey"] is JsonValue key
        && key.TryGetValue<string>(out var s)
        && Guid.TryParse(s, out var guid)
        && guid == RichTextBlockKey;

    private static bool NormaliseTextObject(JsonObject text)
    {
        if (text["markup"] is not JsonValue markupNode || !markupNode.TryGetValue<string>(out var markup))
        {
            return false;
        }

        var normalised = NormaliseMarkup(markup);
        if (normalised == markup)
        {
            return false;
        }

        text["markup"] = normalised;
        return true;
    }

    private static JsonObject? ParseObject(string json)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

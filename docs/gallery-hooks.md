# Gallery hooks

Contract between `src/uShopDemo.Web/wwwroot/js/gallery.js` and the views (ADR-0021). `gallery.js` finds elements by data attribute and does nothing on pages without `[data-gallery]`. Views may change freely as long as these hooks stay.

## Rules

- Change markup and CSS around the hooks.
- Don't edit `gallery.js`.
- Thumbnails are real links to the full image, so the page works without the script (the link opens the image).
- The server renders the first thumbnail with `aria-current="true"`; the main image shows the same image.
- Keep the hooks even when a view hides thumbnails for a single image (hide with CSS or `hidden`, don't remove them).
- Each `[data-gallery]` is independent: a swap only touches its own main image and thumbnails.
- Style the active thumbnail with `[aria-current="true"]`.

## Hooks

| Hook | Script behaviour |
|---|---|
| `[data-gallery]` | Container; one per page section. Scopes the hooks below. |
| `img[data-gallery-main]` | Main image; `src` and `alt` replaced on swap. |
| `a[data-gallery-thumb]` | `href` = full image URL, `data-alt` = alt text; always provide it (if missing, the script falls back to the thumbnail's inner `<img alt>`). Click or Enter: `preventDefault`, swap the main image, set `aria-current="true"` on it and remove it from siblings. Focus stays on the activated thumbnail. Modified clicks (ctrl, meta, shift, alt, non-primary button) are left to the browser. Accessible name comes from its inner `<img alt>`. |

## Example

```html
<div data-gallery>
  <img data-gallery-main src="/media/a.jpg" alt="Front view">
  <ul>
    <li><a data-gallery-thumb href="/media/a.jpg" data-alt="Front view" aria-current="true"><img src="/media/a-thumb.jpg" alt="Front view"></a></li>
    <li><a data-gallery-thumb href="/media/b.jpg" data-alt="Side view"><img src="/media/b-thumb.jpg" alt="Side view"></a></li>
  </ul>
</div>
```

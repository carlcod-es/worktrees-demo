# Cart hooks

Contract between `src/uShopDemo.Web/wwwroot/js/cart.js` and the views. `cart.js` finds elements by data attribute and ignores absent ones. Views may change freely as long as these hooks stay.

## Rules

- Change markup and CSS around the hooks.
- Don't edit `cart.js`.
- Keep `hidden` on `[data-cart-requires-items]` elements; the script unhides them when the cart has lines.
- Keep the `Money.Format` server price display (`Shop/Money.cs`), so the server and the script format identically (`£1,234.56`).
- Money from the script is formatted `en-GB` GBP; dates are `en-GB` medium/short in UTC.

## Global (every page, via `_Layout`)

| Hook | Script behaviour |
|---|---|
| `<meta name="ushop-api" content="...">` | API base URL (`USHOP_API_URL`). Required. |
| `[data-cart-count]` | Text set to the cart item count. If inside `a[aria-label]`, the label becomes `Cart, N item(s)`. |
| `[data-cart-status]` | Live region (`role="status"`). Receives status and error messages; cleared before each update. Don't hide it with `display: none` when empty (screen readers may then miss the next message); restyle the empty state instead. |

## Templates

| Hook | Script behaviour |
|---|---|
| `[data-field="name"]` | Only inside a cloned template: text set from the value for that field name (fields are listed per template below). Unknown fields are left alone. |

## Within a `[data-cart]` container

Used on the cart and checkout views. Each container is rendered independently.

| Hook | Script behaviour |
|---|---|
| `[data-cart]` | Container scoping the hooks below. |
| `[data-cart-total]` | Text set to cart total. |
| `[data-cart-item-count]` | Text set to item count. |
| `[data-cart-requires-items]` | `hidden` set when the cart is empty, removed otherwise. |
| `[data-cart-lines]` | Emptied, then one clone of `template[data-cart-line]` appended per line. |
| `[data-cart-empty-target]` | Optional. Where `template[data-cart-empty]` goes when empty; defaults to `[data-cart-lines]`. Emptied on every render. |
| `template[data-cart-line]` | Line template. Fields: `name`, `sku`, `unitPrice`, `quantity`, `lineTotal`. |
| `template[data-cart-empty]` | Shown when the cart has no lines. |

Within a line template (cart view). All line hooks are optional, with one dependency: `form[data-line-form]` needs a `[data-line-quantity]` in the same template, otherwise its submit is ignored. When the last line is removed, focus moves to the page's first `h1` (given `tabindex="-1"`), so keep an `h1` on the cart view.

| Hook | Script behaviour |
|---|---|
| `[data-line-quantity]` | Number input; value set to the line quantity, `aria-label` set. Quantity 0 removes the line. |
| `form[data-line-form]` | Submit sends the quantity update. |
| `[data-line-update]` | Submit button, disabled while busy. |
| `[data-line-remove]` | Click removes the line; `aria-label` set to `Remove <name>`. The script calls `preventDefault()` on the click, so it is safe as a plain `<button>` inside the line form (it won't also submit the update). |

## Product

| Hook | Notes |
|---|---|
| `form[data-cart-add]` | Submit adds the line. |
| `data-product-key` | Product GUID (`Model.Key`). |
| `data-product-name` | Product title. |
| `data-product-sku` | SKU. |
| `data-product-price` | Invariant-culture decimal (`1234.56`), not formatted. Sent as the unit price. |
| `input[name=quantity]` | Optional; defaults to 1 if absent. The API accepts 1-99. The input's `min`/`max`/`step`/`required` attributes drive the browser validity check (`reportValidity()`) before sending; set them to match. |

## Checkout

| Hook | Notes |
|---|---|
| `form[data-checkout]` | Submit places the order. Use `novalidate`; the API validates. |
| `data-confirmation-url` | On the form. URL of the confirmation page; the script appends `?order=<number>`. |
| Fields `name`, `email`, `address`, `cardNumber` | Control `name` attributes. `cardNumber` is cleared on success. |
| `[data-error-for="<field>"]` | One per field. Receives that field's validation message; the control gets `aria-invalid`. Unmatched errors go to `[data-cart-status]`. |

The checkout view also uses `[data-cart]`, `[data-cart-empty-target]`, `[data-cart-requires-items]`, `[data-cart-lines]`, `[data-cart-total]` and both cart templates as above.

## Confirmation

| Hook | Notes |
|---|---|
| `[data-order]` | Container. The script reads `?order=` from the URL. |
| `[data-order-view]` | Children replaced with the clone of the template for the current state. Required inside `[data-order]`. |
| `template[data-order-pending]` | Field: `orderNumber`. Polled every 500 ms for up to 30 s. |
| `template[data-order-confirmed]` | Fields: `orderNumber`, `name`, `email`, `address`, `total`, `orderedUtc`. |
| `[data-order-lines]` | Inside the confirmed template. Receives one clone of `template[data-order-line]` per line. |
| `template[data-order-line]` | Nested in the confirmed template. Fields: `name`, `quantity`, `lineTotal`. |
| `template[data-order-rejected]` | Fields: `orderNumber`, `reason`. |
| `template[data-order-notfound]` | Shown when `?order=` is missing or the order is unknown. |

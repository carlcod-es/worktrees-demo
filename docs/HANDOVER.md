# Handover: `add-cart-api` and `ushop-demo` additions (2026-11-02)

Branch `bugfixes`, all work committed (`3c1d9d2`, `debe8ea`). Temporary file: delete `docs/HANDOVER.md` when `add-cart-api` is archived (last step under "After your checks").

## Status

| Change | Done | Open |
|---|---|---|
| `add-cart-api` | 14 / 21 tasks | 2.5, 4.4, 5.2, 6.1, 6.2, 6.3, 6.4 |
| `ushop-demo` | 22 / 29 tasks | Section 5 (layout branches), Section 6 (demo docs): not started, wait for `add-cart-api` to be merged |

Every open `add-cart-api` task is built and approved by the reviewer and supervisor. They wait only on the manual checks below (`add-cart-api` task 6.4 waits on one docs fix, see "After your checks").

## What was built

### `add-cart-api` (cart moved out of Umbraco into its own API)
- **Cart API** `src/uShopDemo.Api`: .NET 10 minimal API in the same SQL database as Umbraco (`ushop*` tables, own migrations history).
  - Writes are queued and answered 202 with an operation id; the browser polls the operation.
  - Checkout returns an order number at once; the order reads pending, then confirmed or rejected.
  - Signed session cookie `__Host-ushop-session-<apiPort>` (HttpOnly, Secure, SameSite=Strict, 30 days). A cart belongs to the session that created it; other sessions see it as empty and get 404.
  - CORS allows only the site origin. `POST /session` returns 403 for any origin other than the site or the API itself.
  - 113 in-process tests (SQLite): `dotnet test`.
- **Site** `src/uShopDemo.Web`: the server-side cart code is removed.
  - `wwwroot/js/cart.js` calls the API from the browser and fills view-owned `<template>`s via data attributes (`docs/cart-hooks.md`).
  - Prices are formatted as GBP, `£1,234.56`, on both server and client.
- **Aspire**: new `ushop-api` resource; `ushop-web` starts only after the API's `/health` passes.
- **Launcher** `scripts/dev.mjs`: prints the API URL. Ctrl+C handling rewritten (`add-cart-api` task 4.4):
  - one Ctrl+C stops the whole stack and removes the SQL container;
  - a second Ctrl+C forces exit (may leave the container);
  - exit code 130 after Ctrl+C.
- **API docs** (`add-cart-api` task 2.5, ADR-0018), Development only:
  - `<api>/openapi/v1.json`, and the Scalar UI at `<api>/scalar`;
  - you can start a session in Scalar and try the cart endpoints.

### `ushop-demo`
- Demo notice banner removed from every page and the static error pages (`ushop-demo` task 3.15, ADR-0019).

### New ADRs
- ADR-0018: OpenAPI document and Scalar.
- ADR-0019: no demo notice banner. Supersedes the notice clauses of ADR-0007 and ADR-0015.

## What you need to test

Run from the repo root on `bugfixes`. Docker running, dev cert trusted (`dotnet dev-certs https --trust`). Commands are PowerShell; macOS/Linux equivalents noted where they differ.

Start the stack:

```sh
node scripts/dev.mjs
```

It prints `site`, `api` and `dashboard` URLs. Below, `<site>` and `<api>` mean those URLs. First start takes 1-2 minutes.

### 1. Home page loads cleanly (`add-cart-api` task 5.2)
1. Open `<site>` in Firefox with devtools open (F12, Console tab).
2. Expected: no errors in the console; the header cart count shows `0`; no demo banner at the top.

### 2. Full cart flow (`add-cart-api` task 6.1)
1. Open a product, set quantity 2, click Add to cart.
   - Expected: no page reload; header count shows `2`.
2. Open the cart: change the quantity, then remove a line. Each change appears without a reload.
3. Add something again, go to checkout, leave email empty, submit.
   - Expected: inline error by the email field, no order placed.
4. Fill everything in and submit.
   - Expected: confirmation shows "processing", then "confirmed" with an order number (`US-yyMMdd-NNNNN`) and total. The cart is empty afterwards.
5. The product page, cart and confirmation show amounts in the same `£1,234.56` format.
6. **Ownership check:**
   1. In the first window, in the devtools console run `localStorage.getItem('ushop.cartId')` and copy the value. Also copy the confirmation page URL (it ends `?order=US-...`).
   2. Open a private window and go to `<site>`. In its console run `localStorage.setItem('ushop.cartId', '<copied id>')`, then open the cart page.
      - Expected: the cart is empty.
   3. Open the copied confirmation URL in the private window.
      - Expected: "order not found".
7. **Cookie check:** devtools > Storage > Cookies > `<api>` origin. `__Host-ushop-session-<port>` is HttpOnly, Secure, SameSite=Strict. In the console, `document.cookie` does not contain it.
8. **Database check**, with the stack still running:

   ```powershell
   docker ps --format "{{.Names}}" | Select-String ushop-sql  # macOS/Linux: docker ps --format '{{.Names}}' | grep ushop-sql
   $c = "<container name>"
   $pw = docker exec $c printenv MSSQL_SA_PASSWORD
   docker exec $c /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $pw -C -Q "SELECT name FROM sys.databases; SELECT OrderNumber, Status, Email, Total FROM umbracoDbDSN.dbo.ushopCart WHERE OrderNumber IS NOT NULL"
   ```

   - Expected: the only user database is `umbracoDbDSN` (plus the system databases `master`, `tempdb`, `model`, `msdb`); your order's row is listed.
   - If a column name errors, run `SELECT TOP 5 * FROM umbracoDbDSN.dbo.ushopCart` instead.
9. Run `git grep ushopCart -- src/uShopDemo.Web`.
   - Expected: no output.

### 3. API docs (`add-cart-api` task 2.5)
1. Open `<api>/scalar`. It redirects to `/scalar/v1` and lists Carts, Operations, Orders, Session and Health.
2. In Scalar, run `POST /session`.
   - Expected: 204.
3. Then run `GET /carts/{cartId}` with any GUID (for example `00000000-0000-0000-0000-000000000001`).
   - Expected: 200 with an empty cart.
4. Stop the stack (Ctrl+C), run `aspire run` from the repo root, then repeat steps 1-3:
   - find the API URL on the `ushop-api` resource in the dashboard (`aspire run` prints the dashboard link with its login token);
   - a 403 on `POST /session` here means Aspire's proxy changes the scheme. Report it.
   - Stop with Ctrl+C.

### 4. API down (`add-cart-api` task 6.2)
1. With `node scripts/dev.mjs` running and the site open, stop the API: in the dashboard, `ushop-api` resource > Stop. Or, in a second terminal at the repo root, run `aspire resource ushop-api stop`.
2. Click Add to cart on a product.
   - Expected: the page shows "Could not reach the cart service" and the count doesn't change.
3. Start the API again (dashboard Start, or `aspire resource ushop-api start`).

### 5. Ctrl+C on Windows (`add-cart-api` task 4.4)
1. In Windows Terminal (PowerShell), run `node scripts/dev.mjs` and wait until the site loads.
2. Press Ctrl+C once and wait for the prompt.
3. Check:

   ```powershell
   $LASTEXITCODE                                                   # expected: 130
   Get-Process dotnet, uShopDemo* -ErrorAction SilentlyContinue    # expected: nothing
   docker ps                                                       # expected: no ushop-sql container
   ```

4. Optional: the same in a macOS terminal (`echo $?` → 130, `pgrep -fl uShopDemo` → nothing).

### 6. Two instances side by side (`add-cart-api` task 6.3)
`worktrees.mjs up` branches from `main`, which doesn't have the API yet, so make a temporary worktree from `bugfixes` by hand. Each instance needs about 2 GB of Docker memory.

```sh
git worktree add -b test/cart-b ../worktrees-demo.cart-b bugfixes
```

1. Terminal 1, in this repo: `node scripts/dev.mjs`.
2. Terminal 2: `cd ../worktrees-demo.cart-b`, then `node scripts/dev.mjs`. The ports must differ from terminal 1.
3. Open both sites in two tabs of the same browser and add different items in each.
   - Expected: devtools Network shows each site calling its own `<api>` port.
   - Expected: Storage shows two cookies with different port suffixes.
4. Switch between the tabs a few times and reload.
   - Expected: neither cart resets.
5. Place an order in tab 1. Run the database check from test 2, step 8, against each instance's SQL container.
   - Expected: the order is only in instance 1's `ushopCart`.
6. Clean up: Ctrl+C both, then

   ```sh
   git worktree remove ../worktrees-demo.cart-b
   git branch -D test/cart-b
   ```

## Report back

For each test, "pass" or what you saw. Paste exact errors (console, terminal output, status codes).

## After your checks (Architect)
- Tick `add-cart-api` tasks 2.5, 4.4, 5.2, 6.1, 6.2 and 6.3 for each pass. Send any failure to the owning worker, then a review.
- `add-cart-api` task 6.4: one docs fix first. The README sentence about a second Ctrl+C belongs to the launcher, not `aspire run`. Then review and tick.
- Supervisor review of `add-cart-api` Section 5, once test 1 passes. Known items to include:
  - `cart.js` throws on submit if a cart line form lacks `[data-line-quantity]`;
  - focus moves to the page body when a checkout 400 has no field errors.
- Then: report `add-cart-api` complete. To archive, archive `ushop-demo` first, because `add-cart-api` builds on its specs. Merge `bugfixes` to `main` before creating the layout branches (`ushop-demo` Section 5).
- Finally: delete `docs/HANDOVER.md` (`git rm docs/HANDOVER.md`) in the same commit as the archive.

## Logged follow-ups (not blocking)
- Remove the `System.Security.Cryptography.Pkcs` and `.Xml` version pins once upstream packages are patched.
- Tests:
  - `Origin: null` → 403;
  - null check on the Scalar redirect `Location`;
  - the no-body check on error responses also passes on an empty body object;
  - duplicate wait helper in `SessionTests`;
  - duplicate older 401 test;
  - `CorsTests` disposes a client the factory owns;
  - no test that server and client money formatting match.
- The card-number log scan covers `Cart` only.
- A stale EF log filter in appsettings (kept on purpose).
- Launcher:
  - a comment in `shutdown.mjs` wraps unevenly;
  - the 500 ms duplicate-Ctrl+C window uses `Date.now` (`performance.now` would be steadier);
  - label padding in `dev.mjs` output.

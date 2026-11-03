# Follow-ups

Non-blocking items logged during `add-cart-api` reviews. Promote an item to an OpenSpec change or task before fixing it.

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

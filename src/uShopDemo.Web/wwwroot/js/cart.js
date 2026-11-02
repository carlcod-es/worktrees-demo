// Cart client. ES module, no framework. Finds views' hooks by data attribute and ignores absent ones.
const NETWORK_MESSAGE = 'Could not reach the cart service.';
const GENERIC_MESSAGE = 'Something went wrong with the cart. Please try again.';
const money = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'GBP' });
const dateFormat = new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'UTC' });
const CART_KEY = 'ushop.cartId';
const REQUEST_TIMEOUT_MS = 10000;
const TIMEOUT_MESSAGE = 'The cart service is taking too long to respond. Check your cart before trying again.';
const OPERATION_TIMEOUT_MESSAGE = 'The cart update is taking too long. Check your cart before trying again.';
const OPERATION_DEADLINE_MS = 15000;

const base = (document.querySelector('meta[name="ushop-api"]')?.content ?? '').trim().replace(/\/+$/, '');
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];

class CartError extends Error {
  constructor(kind, message, errors) {
    super(message);
    this.kind = kind; // network | notfound | validation | failed | timeout | unexpected
    this.errors = errors;
  }
}

// ---- status ----
const statusTimers = new WeakMap();
function setStatus(message) {
  for (const el of $$('[data-cart-status]')) {
    clearTimeout(statusTimers.get(el));
    el.textContent = '';
    if (message) statusTimers.set(el, setTimeout(() => { el.textContent = message; }, 50));
  }
}

function reportError(error) {
  if (error instanceof CartError) {
    if (error.kind === 'network') return setStatus(NETWORK_MESSAGE);
    if (error.kind === 'validation') {
      const messages = Object.values(error.errors ?? {}).flat();
      return setStatus(messages.length ? messages.join(' ') : GENERIC_MESSAGE);
    }
    return setStatus(error.message || GENERIC_MESSAGE);
  }
  return setStatus(GENERIC_MESSAGE);
}

// ---- cart id ----
let memoryCartId = null;
function newCartId() {
  const id = crypto.randomUUID();
  try { localStorage.setItem(CART_KEY, id); } catch { memoryCartId = id; }
  return id;
}
function getCartId() {
  try {
    return localStorage.getItem(CART_KEY) ?? newCartId();
  } catch {
    return memoryCartId ?? newCartId();
  }
}

// ---- transport ----
const session = (async () => {
  let res;
  try {
    res = await fetch(`${base}/session`, { method: 'POST', credentials: 'include', signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS) });
  } catch {
    // Timeouts are reported as 'network' too. `session` is a one-shot promise, so the page needs a reload to recover.
    throw new CartError('network', NETWORK_MESSAGE);
  }
  if (!res.ok) throw new CartError('unexpected', NETWORK_MESSAGE);
})();
session.catch(() => {}); // handled in init

/** `timeoutMs` bounds the request, including reading the response body. */
async function send(method, path, body, { timeoutMs = REQUEST_TIMEOUT_MS } = {}) {
  await session;
  const init = { method, credentials: 'include', signal: AbortSignal.timeout(timeoutMs) };
  if (body !== undefined) {
    init.headers = { 'Content-Type': 'application/json' };
    init.body = JSON.stringify(body);
  }
  try {
    return await fetch(base + path, init);
  } catch (error) {
    if (error?.name === 'TimeoutError') throw new CartError('timeout', TIMEOUT_MESSAGE);
    throw new CartError('network', NETWORK_MESSAGE);
  }
}

async function readJson(res) {
  try {
    return await res.json();
  } catch (error) {
    if (error?.name === 'TimeoutError') throw new CartError('timeout', TIMEOUT_MESSAGE);
    throw new CartError('unexpected', GENERIC_MESSAGE);
  }
}

/** Throws CartError for any non-202 write response. Returns the parsed 202 body. */
async function accept(res) {
  if (res.status === 202) return readJson(res);
  if (res.status === 404) throw new CartError('notfound', 'Your cart was reset. Please try again.');
  if (res.status === 400) {
    let errors = {};
    try {
      errors = (await res.json()).errors ?? {};
    } catch (error) {
      if (error?.name === 'TimeoutError') throw new CartError('timeout', TIMEOUT_MESSAGE);
      /* otherwise ignore an unreadable body */
    }
    throw new CartError('validation', 'Please check the details and try again.', errors);
  }
  throw new CartError('unexpected', GENERIC_MESSAGE);
}

async function waitForOperation(operationId) {
  const deadline = Date.now() + OPERATION_DEADLINE_MS;
  while (Date.now() < deadline) {
    const timeoutMs = Math.min(REQUEST_TIMEOUT_MS, Math.max(deadline - Date.now(), 1000));
    let op;
    try {
      const res = await send('GET', `/operations/${encodeURIComponent(operationId)}`, undefined, { timeoutMs });
      if (!res.ok) throw new CartError('unexpected', GENERIC_MESSAGE);
      op = await readJson(res);
    } catch (error) {
      // A timeout clamped by the operation deadline is the operation running out of time.
      if (error instanceof CartError && error.kind === 'timeout' && timeoutMs < REQUEST_TIMEOUT_MS) {
        throw new CartError('timeout', OPERATION_TIMEOUT_MESSAGE);
      }
      throw error;
    }
    if (op.status === 'completed') return;
    if (op.status === 'failed') throw new CartError('failed', op.error || 'The cart update failed.');
    if (op.status !== 'queued') throw new CartError('unexpected', GENERIC_MESSAGE);
    await sleep(250);
  }
  throw new CartError('timeout', OPERATION_TIMEOUT_MESSAGE);
}

/** build(cartId) -> [method, path, body?]. Sends, then polls until the operation completes. */
async function write(build) {
  const [method, path, body] = build(getCartId());
  const accepted = await accept(await send(method, path, body));
  await waitForOperation(accepted.operationId);
}

/** Write that, on a 404 (cart owned by another session), swaps the cart id. Adds retry once. */
async function writeWithReset(build, { retry }) {
  try {
    await write(build);
  } catch (error) {
    if (!(error instanceof CartError) || error.kind !== 'notfound') throw error;
    newCartId();
    if (retry) await write(build);
    else throw error;
  }
}

async function readCart() {
  const res = await send('GET', `/carts/${encodeURIComponent(getCartId())}`);
  if (!res.ok) throw new CartError('unexpected', GENERIC_MESSAGE);
  return readJson(res);
}

// ---- busy state ----
/** `action` may return a function; it runs after controls are restored and replaces the default trigger refocus. */
async function busy(trigger, form, action) {
  const targets = new Set([trigger, form].filter(Boolean));
  const controls = new Set(trigger ? [trigger] : []);
  if (form) for (const c of form.querySelectorAll('button, input, select, textarea')) controls.add(c);
  const wasDisabled = new Map([...controls].map((c) => [c, c.disabled]));
  const hadFocus = trigger && document.activeElement === trigger;
  for (const t of targets) t.setAttribute('aria-busy', 'true');
  for (const c of controls) c.disabled = true;
  let after;
  try {
    after = await action();
  } finally {
    for (const t of targets) t.removeAttribute('aria-busy');
    for (const [c, d] of wasDisabled) c.disabled = d;
    if (typeof after === 'function') after();
    else if (hadFocus && trigger.isConnected) trigger.focus();
  }
}

// ---- rendering ----
function fill(root, values) {
  for (const el of $$('[data-field]', root)) {
    const value = values[el.dataset.field];
    if (value !== undefined) el.textContent = String(value);
  }
}

function updateCount(count) {
  for (const el of $$('[data-cart-count]')) {
    el.textContent = String(count);
    const link = el.closest('a[aria-label]');
    if (link) link.setAttribute('aria-label', `Cart, ${count} ${count === 1 ? 'item' : 'items'}`);
  }
}

function renderCart(cart) {
  updateCount(cart.itemCount);
  const empty = cart.lines.length === 0;
  for (const container of $$('[data-cart]')) {
    for (const el of $$('[data-cart-total]', container)) el.textContent = money.format(cart.total);
    for (const el of $$('[data-cart-item-count]', container)) el.textContent = String(cart.itemCount);
    for (const el of $$('[data-cart-requires-items]', container)) el.hidden = empty;

    const linesTarget = container.querySelector('[data-cart-lines]');
    const emptyTarget = container.querySelector('[data-cart-empty-target]') ?? linesTarget;
    linesTarget?.replaceChildren();
    if (emptyTarget !== linesTarget) emptyTarget.replaceChildren();

    if (empty) {
      const template = container.querySelector('template[data-cart-empty]');
      if (template && emptyTarget) emptyTarget.append(template.content.cloneNode(true));
      continue;
    }
    const template = container.querySelector('template[data-cart-line]');
    if (!template || !linesTarget) continue;
    for (const line of cart.lines) {
      const node = template.content.cloneNode(true);
      fill(node, {
        name: line.name,
        sku: line.sku,
        unitPrice: money.format(line.unitPrice),
        quantity: line.quantity,
        lineTotal: money.format(line.lineTotal),
      });
      bindLine(node, line);
      linesTarget.append(node);
    }
  }
}

function bindLine(node, line) {
  const input = node.querySelector('[data-line-quantity]');
  if (input) {
    input.value = String(line.quantity);
    input.setAttribute('aria-label', `Quantity for ${line.name}`);
  }
  node.querySelector('[data-line-remove]')?.setAttribute('aria-label', `Remove ${line.name}`);
  const form = node.querySelector('[data-line-form]');
  const update = node.querySelector('[data-line-update]');
  const remove = node.querySelector('[data-line-remove]');
  const key = encodeURIComponent(line.productKey);

  if (input) input.dataset.productKey = line.productKey;

  // The row is re-rendered, so the triggering element is gone; the returned callback runs after
  // busy() re-enables controls and moves focus to the equivalent element in the new DOM.
  const run = (trigger, scope, build, doneMessage, refocus) => {
    const inputs = $$('[data-line-quantity]');
    const index = inputs.indexOf(input);
    return busy(trigger, scope, async () => {
      try {
        await writeWithReset(build, { retry: false });
        await refreshCart();
        setStatus(doneMessage);
      } catch (error) {
        reportError(error);
        if (error instanceof CartError && (error.kind === 'notfound' || error.kind === 'timeout')) await refreshCart().catch(() => {});
      }
      return () => refocus(index);
    });
  };

  form?.addEventListener('submit', (event) => {
    event.preventDefault();
    if (!input || !input.reportValidity()) return;
    const quantity = Number(input.value);
    run(event.submitter ?? update, form, (id) => ['PUT', `/carts/${encodeURIComponent(id)}/lines/${key}`, { quantity }], 'Cart updated.',
      (index) => (focusQuantity(line.productKey) || focusNear(index)));
  });
  remove?.addEventListener('click', (event) => {
    event.preventDefault(); // a plain <button> inside the line form would also submit it
    run(remove, form ?? null, (id) => ['DELETE', `/carts/${encodeURIComponent(id)}/lines/${key}`], `Removed ${line.name} from the cart.`,
      (index) => {
        if (remove.isConnected) return remove.focus(); // removal failed, row untouched
        focusNear(index);
      });
  });
}

function focusQuantity(productKey) {
  const input = $$('[data-line-quantity]').find((el) => el.dataset.productKey === productKey);
  input?.focus();
  return Boolean(input);
}

/** Focus the quantity input now at `index` (the next line), else the last (previous line), else the page heading. */
function focusNear(index) {
  const inputs = $$('[data-line-quantity]');
  const target = inputs[Math.min(Math.max(index, 0), inputs.length - 1)];
  if (target) return target.focus();
  const heading = document.querySelector('h1');
  if (!heading) return;
  if (!heading.hasAttribute('tabindex')) heading.setAttribute('tabindex', '-1');
  heading.focus();
}

async function refreshCart() {
  renderCart(await readCart());
}

// ---- add form ----
function initAddForms() {
  for (const form of $$('form[data-cart-add]')) {
    form.addEventListener('submit', (event) => {
      event.preventDefault();
      const input = form.elements.quantity;
      if (input && !input.reportValidity()) return;
      const line = {
        productKey: form.dataset.productKey,
        name: form.dataset.productName,
        sku: form.dataset.productSku,
        unitPrice: Number.parseFloat(form.dataset.productPrice),
        quantity: input ? Number(input.value) : 1,
      };
      busy(event.submitter ?? form.querySelector('[type="submit"]'), form, async () => {
        try {
          await writeWithReset((id) => ['POST', `/carts/${encodeURIComponent(id)}/lines`, line], { retry: true });
          renderCart(await readCart());
          setStatus('Added to cart.');
        } catch (error) {
          reportError(error);
          if (error instanceof CartError && error.kind === 'timeout') await refreshCart().catch(() => {});
        }
      });
    });
  }
}

// ---- checkout ----
function initCheckout() {
  for (const form of $$('form[data-checkout]')) {
    const slot = (name) => form.querySelector(`[data-error-for="${name}"]`);
    form.addEventListener('submit', (event) => {
      event.preventDefault();
      for (const el of $$('[data-error-for]', form)) el.textContent = '';
      for (const el of $$('[aria-invalid]', form)) el.removeAttribute('aria-invalid');
      setStatus('');
      const data = new FormData(form);
      const payload = Object.fromEntries(['name', 'email', 'address', 'cardNumber'].map((k) => [k, String(data.get(k) ?? '')]));

      const trigger = event.submitter ?? form.querySelector('[type="submit"]');
      busy(trigger, form, async () => {
        try {
          const res = await send('POST', `/carts/${encodeURIComponent(getCartId())}/checkout`, payload);
          if (res.status === 400) {
            let errors = {};
            try { errors = (await res.json()).errors ?? {}; } catch (error) { if (error?.name === 'TimeoutError') throw new CartError('timeout', TIMEOUT_MESSAGE); }
            // Runs after busy() re-enables the controls, so focus() takes effect.
            return () => showCheckoutErrors(form, errors, trigger);
          }
          const accepted = await accept(res);
          if (!accepted?.orderNumber) throw new CartError('unexpected', GENERIC_MESSAGE);
          if (form.elements.cardNumber) form.elements.cardNumber.value = '';
          newCartId();
          const url = form.dataset.confirmationUrl || '/';
          location.assign(`${url}${url.includes('?') ? '&' : '?'}order=${encodeURIComponent(accepted.orderNumber)}`);
        } catch (error) {
          reportError(error);
          if (error instanceof CartError && error.kind === 'notfound') {
            newCartId();
            await refreshCart().catch(() => {});
          }
        }
      });
    });

    function showCheckoutErrors(formEl, errors, trigger) {
      const unmatched = [];
      const invalid = new Set();
      for (const [key, messages] of Object.entries(errors)) {
        const name = ['name', 'email', 'address', 'cardNumber'].find((n) => n.toLowerCase() === key.toLowerCase());
        const target = name && slot(name);
        if (!target) { unmatched.push(...messages); continue; }
        target.textContent = messages.join(' ');
        formEl.elements[name]?.setAttribute('aria-invalid', 'true');
        invalid.add(name);
      }
      if (unmatched.length) setStatus(unmatched.join(' '));
      else if (!invalid.size) setStatus(GENERIC_MESSAGE);
      const first = [...formEl.elements].find((el) => el.name && invalid.has(el.name));
      (first ?? trigger)?.focus();
    }
  }
}

// ---- confirmation ----
function initOrder() {
  const container = document.querySelector('[data-order]');
  if (!container) return;
  const view = container.querySelector('[data-order-view]');
  const show = (name, values = {}) => {
    const template = container.querySelector(`template[data-order-${name}]`);
    view.replaceChildren();
    if (!template) return null;
    const node = template.content.cloneNode(true);
    fill(node, values);
    view.append(node);
    return view;
  };

  const number = new URLSearchParams(location.search).get('order');
  if (!number) { show('notfound'); return; }

  (async () => {
    const deadline = Date.now() + 30000;
    let shownPending = false;
    for (;;) {
      const res = await send('GET', `/orders/${encodeURIComponent(number)}`);
      if (res.status === 404) return show('notfound');
      if (!res.ok) throw new CartError('unexpected', GENERIC_MESSAGE);
      const order = await readJson(res);
      if (order.status === 'confirmed') {
        const root = show('confirmed', {
          orderNumber: order.orderNumber,
          name: order.name,
          email: order.email,
          address: order.address,
          total: money.format(order.total),
          orderedUtc: order.orderedUtc ? `${dateFormat.format(new Date(order.orderedUtc))} UTC` : '',
        });
        const list = root?.querySelector('[data-order-lines]');
        const template = root?.querySelector('template[data-order-line]');
        if (list && template) {
          for (const line of order.lines ?? []) {
            const node = template.content.cloneNode(true);
            fill(node, { name: line.name, quantity: line.quantity, lineTotal: money.format(line.lineTotal) });
            list.append(node);
          }
        }
        return;
      }
      if (order.status === 'rejected') return show('rejected', { orderNumber: order.orderNumber, reason: order.reason });
      if (order.status !== 'pending') throw new CartError('unexpected', GENERIC_MESSAGE);
      if (!shownPending) { show('pending', { orderNumber: order.orderNumber }); shownPending = true; }
      if (Date.now() >= deadline) {
        setStatus('Your order is still being processed. Refresh this page to check again.');
        return;
      }
      await sleep(500);
    }
  })().catch(reportError);
}

// ---- init ----
async function init() {
  // Handlers first so a fast submit is never a full page post; their requests wait on the session.
  initAddForms();
  initCheckout();
  try {
    await session;
  } catch {
    setStatus(NETWORK_MESSAGE);
    return;
  }
  initOrder();
  try {
    await refreshCart();
  } catch (error) {
    reportError(error);
  }
}

init();

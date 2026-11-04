// Mobile navigation disclosure. ES module, no framework. No-op on pages without [data-nav].
// Hooks: [data-nav] (container), [data-nav-toggle] (button), [data-nav-menu] (shown/hidden).
// CSS decides visibility per viewport; this script never reads the viewport.
// Escape is handled on document so it closes an open menu regardless of where focus is.
const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];

function init() {
  for (const nav of $$('[data-nav]')) {
    const toggle = nav.querySelector('[data-nav-toggle]');
    const menu = nav.querySelector('[data-nav-menu]');
    if (!toggle || !menu) continue;

    const setOpen = (open) => {
      toggle.setAttribute('aria-expanded', String(open));
      nav.toggleAttribute('data-nav-open', open);
    };

    nav.setAttribute('data-nav-ready', '');
    toggle.hidden = false;

    toggle.addEventListener('click', () => {
      setOpen(toggle.getAttribute('aria-expanded') !== 'true');
    });

    document.addEventListener('keydown', (event) => {
      if (event.key !== 'Escape' || event.defaultPrevented) return;
      if (toggle.getAttribute('aria-expanded') !== 'true') return;
      setOpen(false);
      toggle.focus();
    });
  }
}

init();

// Product gallery. ES module, no framework. No-op on pages without [data-gallery].
// Thumbnails are plain links, so without this script they open the full image.
const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];

function init() {
  for (const gallery of $$('[data-gallery]')) {
    // One delegated listener per container; Enter on a focused link also fires `click`.
    gallery.addEventListener('click', (event) => {
      if (event.defaultPrevented || event.button !== 0) return;
      if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return; // keep "open in new tab"
      const thumb = event.target instanceof Element ? event.target.closest('a[data-gallery-thumb]') : null;
      if (!thumb || !gallery.contains(thumb)) return;
      // A nested gallery owns its own thumbs.
      if (thumb.closest('[data-gallery]') !== gallery) return;
      const main = gallery.querySelector('img[data-gallery-main]');
      if (!main) return;

      event.preventDefault();
      main.src = thumb.href;
      main.alt = thumb.dataset.alt ?? thumb.querySelector('img')?.alt ?? '';
      for (const other of $$('a[data-gallery-thumb]', gallery)) {
        if (other === thumb) other.setAttribute('aria-current', 'true');
        else other.removeAttribute('aria-current');
      }
    });
  }
}

init();

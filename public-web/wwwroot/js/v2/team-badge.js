(() => {
  function activateFallback(badge) {
    if (!badge) return;
    badge.classList.add('is-fallback');
    badge.classList.remove('has-logo');
    const img = badge.querySelector('[data-v2-team-logo]');
    if (img) {
      img.setAttribute('hidden', '');
      img.removeAttribute('src');
    }
  }

  function bindLogo(img) {
    if (!(img instanceof HTMLImageElement)) return;
    const badge = img.closest('[data-v2-team-badge]');
    if (!badge) return;

    const fail = () => activateFallback(badge);

    const onError = () => {
      const full = img.getAttribute('data-full-logo');
      if (full && img.getAttribute('src') !== full) {
        img.removeAttribute('data-full-logo');
        img.addEventListener('error', fail, { once: true });
        img.setAttribute('src', full);
        return;
      }
      fail();
    };

    img.addEventListener('error', onError, { once: true });

    // Cached/broken image may already be in error state before listener binds.
    if (img.complete && img.naturalWidth === 0) {
      onError();
    }
  }

  function init(root) {
    (root || document).querySelectorAll('[data-v2-team-logo]').forEach(bindLogo);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => init());
  } else {
    init();
  }

  // Expose for dynamically injected markup if needed later.
  window.v2BindTeamBadges = init;
})();

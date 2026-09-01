(() => {
  const STORAGE_PREFIX = 'miliga-action-help:';
  const THIRTY_DAYS_MS = 30 * 24 * 60 * 60 * 1000;

  function storageKey(root) {
    return `${STORAGE_PREFIX}${root.getAttribute('data-help-key') || location.pathname}`;
  }

  function readLastShown(root) {
    try {
      return Number(localStorage.getItem(storageKey(root)) || '0');
    } catch {
      return 0;
    }
  }

  function markShown(root) {
    try {
      localStorage.setItem(storageKey(root), String(Date.now()));
    } catch {
      /* ignore quota / private mode */
    }
  }

  function canShowByCadence(root) {
    if (!root.hidden) return true;
    const lastShown = readLastShown(root);
    return !lastShown || Date.now() - lastShown >= THIRTY_DAYS_MS;
  }

  function actionRoots(root) {
    const container = root.closest('.v2-league-hero__actions') || document;
    return {
      follow: container.querySelector('[data-v2-follow]'),
      pin: container.querySelector('[data-home-league-pin]')
    };
  }

  function isVisible(el) {
    return el instanceof HTMLElement && !el.hidden && el.offsetParent !== null;
  }

  function messages(root) {
    const { follow, pin } = actionRoots(root);
    const items = [];

    if (isVisible(follow) && follow.dataset.state !== 'following') {
      items.push('<strong>Seguir liga</strong> activa avisos de resultados y cambios del fixture.');
    }

    if (pin instanceof HTMLElement && pin.dataset.state !== 'pinned') {
      items.push('<strong>Liga de inicio</strong> hace que MiLiga abra directo en esta liga.');
    }

    return items;
  }

  function hide(root) {
    root.hidden = true;
  }

  function render(root) {
    if (!canShowByCadence(root)) {
      hide(root);
      return;
    }

    const body = root.querySelector('[data-v2-action-help-body]');
    if (!body) return;

    const items = messages(root);
    if (!items.length) {
      hide(root);
      return;
    }

    body.innerHTML = items.map((item) => `<p>${item}</p>`).join('');
    root.hidden = false;
    markShown(root);
  }

  function bind(root) {
    if (!(root instanceof HTMLElement) || root.dataset.bound === '1') return;
    root.dataset.bound = '1';

    const close = root.querySelector('[data-v2-action-help-close]');
    if (close) {
      close.addEventListener('click', () => {
        markShown(root);
        hide(root);
      });
    }

    document.addEventListener('v2-follow-state', () => render(root));
    document.addEventListener('v2-home-pin-state', () => render(root));
    window.setTimeout(() => render(root), 500);
  }

  function init(scope) {
    (scope || document).querySelectorAll('[data-v2-action-help]').forEach(bind);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => init());
  } else {
    init();
  }
})();

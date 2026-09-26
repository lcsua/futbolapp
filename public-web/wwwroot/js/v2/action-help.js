(() => {
  const STORAGE_PREFIX = 'miliga-league-cta:';
  const THIRTY_DAYS_MS = 30 * 24 * 60 * 60 * 1000;

  const COPY = {
    both: {
      title: 'No te pierdas nada de esta liga',
      text: 'Seguí la liga para avisos de resultados y fixture. Liga de inicio hace que MiLiga abra directo acá.'
    },
    follow: {
      title: 'No te pierdas nada de esta liga',
      text: 'Recibí avisos de resultados y del fixture cuando haya novedades.'
    },
    pin: {
      title: 'Hacé de esta tu liga de inicio',
      text: 'Así MiLiga abre directo en esta liga, sin pasar por el listado.'
    }
  };

  function storageKey(root) {
    return `${STORAGE_PREFIX}${root.getAttribute('data-help-key') || location.pathname}`;
  }

  function readLastDismissed(root) {
    try {
      return Number(localStorage.getItem(storageKey(root)) || '0');
    } catch {
      return 0;
    }
  }

  function markDismissed(root) {
    try {
      localStorage.setItem(storageKey(root), String(Date.now()));
    } catch {
      /* ignore quota / private mode */
    }
  }

  function dismissedRecently(root) {
    const last = readLastDismissed(root);
    return last > 0 && Date.now() - last < THIRTY_DAYS_MS;
  }

  function followRoot(root) {
    return root.querySelector('[data-cta-follow] [data-v2-follow]');
  }

  function pinRoot(root) {
    return root.querySelector('[data-cta-pin] [data-home-league-pin]');
  }

  function needsFollow(root) {
    const follow = followRoot(root);
    if (!(follow instanceof HTMLElement)) return false;
    const state = follow.dataset.state;
    if (state === 'following' || state === 'unsupported') return false;
    if (follow.hidden && state) return false;
    return true;
  }

  function needsPin(root) {
    const pin = pinRoot(root);
    if (!(pin instanceof HTMLElement)) return false;
    return pin.dataset.state !== 'pinned';
  }

  function setHidden(el, hidden) {
    if (!(el instanceof HTMLElement)) return;
    el.hidden = hidden;
  }

  function hide(root) {
    root.hidden = true;
  }

  function render(root) {
    if (dismissedRecently(root)) {
      hide(root);
      return;
    }

    const showFollow = needsFollow(root);
    const showPin = needsPin(root);
    if (!showFollow && !showPin) {
      hide(root);
      return;
    }

    const mode = showFollow && showPin ? 'both' : showFollow ? 'follow' : 'pin';
    const copy = COPY[mode];
    const title = root.querySelector('[data-cta-title]');
    const text = root.querySelector('[data-cta-text]');
    if (title) title.textContent = copy.title;
    if (text) text.textContent = copy.text;

    root.dataset.mode = mode;
    setHidden(root.querySelector('[data-cta-follow]'), !showFollow);
    setHidden(root.querySelector('[data-cta-pin]'), !showPin);
    setHidden(root.querySelector('[data-cta-icon="follow"]'), mode === 'pin');
    setHidden(root.querySelector('[data-cta-icon="pin"]'), mode !== 'pin');
    root.hidden = false;
  }

  function bind(root) {
    if (!(root instanceof HTMLElement) || root.dataset.bound === '1') return;
    root.dataset.bound = '1';

    const close = root.querySelector('[data-v2-league-cta-close]');
    if (close) {
      close.addEventListener('click', () => {
        markDismissed(root);
        hide(root);
      });
    }

    document.addEventListener('v2-follow-state', () => render(root));
    document.addEventListener('v2-home-pin-state', () => render(root));
    render(root);
    window.setTimeout(() => render(root), 500);
  }

  function init(scope) {
    (scope || document).querySelectorAll('[data-v2-league-cta]').forEach(bind);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => init());
  } else {
    init();
  }
})();

(() => {
  const PINNED_KEY = 'miliga-home-league';
  const LAST_KEY = 'miliga-last-league';
  const PATH_RE = /^(argentina\/)?[a-z0-9]+(?:-[a-z0-9]+)*$/;
  const LABEL_OFF = 'Fijar inicio';
  const LABEL_ON = 'Liga de inicio';

  function configuredPathBase() {
    return (document.documentElement.getAttribute('data-path-base') || '').replace(/\/$/, '');
  }

  function publicBase() {
    const base = configuredPathBase();
    if (!base) return '';
    const path = location.pathname;
    if (path === base || path.startsWith(`${base}/`)) return base;
    return '';
  }

  function readStorage(key) {
    try {
      return localStorage.getItem(key) || '';
    } catch {
      return '';
    }
  }

  function writeStorage(key, value) {
    try {
      if (value) localStorage.setItem(key, value);
      else localStorage.removeItem(key);
    } catch {
      /* ignore quota / private mode */
    }
  }

  function isValidPath(value) {
    return typeof value === 'string' && PATH_RE.test(value);
  }

  function serverPinned() {
    return document.documentElement.getAttribute('data-pinned-home-league') || '';
  }

  function homeTarget() {
    const pinned = readStorage(PINNED_KEY) || serverPinned();
    if (isValidPath(pinned)) return pinned;
    const last = readStorage(LAST_KEY) || document.documentElement.getAttribute('data-home-league') || '';
    return isValidPath(last) ? last : '';
  }

  function leagueHref(path) {
    return `${publicBase()}/ligas/${path}`;
  }

  function persistPinned(slug) {
    return fetch(`${publicBase()}/liga-inicio`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      credentials: 'same-origin',
      body: JSON.stringify({ slug: slug || '' })
    }).catch(() => {});
  }

  function rememberCurrent() {
    const path = document.documentElement.getAttribute('data-home-league') || '';
    if (!isValidPath(path)) return;
    writeStorage(LAST_KEY, path);
  }

  function applyHomeLinks() {
    const target = homeTarget();
    if (!target) return;
    const href = leagueHref(target);
    document.querySelectorAll('[data-v2-home-link]').forEach((link) => {
      link.setAttribute('href', href);
    });
  }

  function setPinUi(root, pinned) {
    const btn = root.querySelector('[data-home-league-pin-trigger]');
    const label = root.querySelector('[data-home-league-pin-label]');
    if (!btn) return;
    btn.classList.toggle('is-pinned', pinned);
    btn.setAttribute('aria-pressed', pinned ? 'true' : 'false');
    btn.setAttribute(
      'aria-label',
      pinned ? 'Dejar de abrir esta liga al entrar' : 'Usar esta liga al abrir Mi Liga'
    );
    if (label) label.textContent = pinned ? LABEL_ON : LABEL_OFF;
  }

  function isPinned(path) {
    return readStorage(PINNED_KEY) === path || serverPinned() === path;
  }

  function bindPin(root) {
    const path = root.getAttribute('data-home-league-pin') || '';
    if (!isValidPath(path)) return;
    setPinUi(root, isPinned(path));
    const btn = root.querySelector('[data-home-league-pin-trigger]');
    if (!btn || btn.dataset.bound === '1') return;
    btn.dataset.bound = '1';
    btn.addEventListener('click', () => {
      const next = !isPinned(path);
      writeStorage(PINNED_KEY, next ? path : '');
      if (next) writeStorage(LAST_KEY, path);
      document.documentElement.setAttribute('data-pinned-home-league', next ? path : '');
      setPinUi(root, next);
      applyHomeLinks();
      persistPinned(next ? path : '');
    });
  }

  function registerWorker() {
    if (!('serviceWorker' in navigator)) return;
    const scope = publicBase() ? `${publicBase()}/` : '/';
    navigator.serviceWorker.register(`${publicBase()}/sw.js`, { scope }).catch(() => {});
  }

  rememberCurrent();
  applyHomeLinks();
  document.querySelectorAll('[data-home-league-pin]').forEach(bindPin);
  registerWorker();
})();

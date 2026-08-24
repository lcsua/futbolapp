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

  function readCookie(name) {
    const prefix = `${name}=`;
    const parts = document.cookie ? document.cookie.split('; ') : [];
    for (const part of parts) {
      if (part.startsWith(prefix)) {
        return decodeURIComponent(part.slice(prefix.length));
      }
    }
    return '';
  }

  function writeCookie(name, value) {
    const secure = location.protocol === 'https:' ? '; Secure' : '';
    if (value) {
      document.cookie = `${name}=${encodeURIComponent(value)}; Path=/; Max-Age=31536000; SameSite=Lax${secure}`;
      return;
    }
    document.cookie = `${name}=; Path=/; Max-Age=0; SameSite=Lax${secure}`;
  }

  function read(key) {
    return readStorage(key) || readCookie(key);
  }

  function write(key, value) {
    writeStorage(key, value);
    writeCookie(key, value);
  }

  function isValidPath(value) {
    return typeof value === 'string' && PATH_RE.test(value);
  }

  function homeTarget() {
    const pinned = read(PINNED_KEY);
    if (isValidPath(pinned)) return pinned;
    const last = read(LAST_KEY);
    return isValidPath(last) ? last : '';
  }

  function leagueHref(path) {
    return `${publicBase()}/ligas/${path}`;
  }

  function rememberCurrent() {
    const path = document.documentElement.getAttribute('data-home-league') || '';
    if (!isValidPath(path)) return;
    write(LAST_KEY, path);
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

  function bindPin(root) {
    const path = root.getAttribute('data-home-league-pin') || '';
    if (!isValidPath(path)) return;
    setPinUi(root, read(PINNED_KEY) === path);
    const btn = root.querySelector('[data-home-league-pin-trigger]');
    if (!btn || btn.dataset.bound === '1') return;
    btn.dataset.bound = '1';
    btn.addEventListener('click', () => {
      const pinnedNow = read(PINNED_KEY) === path;
      write(PINNED_KEY, pinnedNow ? '' : path);
      if (!pinnedNow) write(LAST_KEY, path);
      setPinUi(root, !pinnedNow);
      applyHomeLinks();
    });
  }

  rememberCurrent();
  applyHomeLinks();
  document.querySelectorAll('[data-home-league-pin]').forEach(bindPin);
})();

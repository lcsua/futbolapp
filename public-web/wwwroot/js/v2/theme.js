(() => {
  const ROOT_ATTR = 'data-v2-theme';
  const STORAGE_KEY = 'miliga-v2-theme';
  const DEFAULT_THEME = 'blue';

  function normalize(theme) {
    return theme === 'green' || theme === 'blue' ? theme : DEFAULT_THEME;
  }

  function brandBase(pathBase, theme) {
    const base = (pathBase || '').replace(/\/$/, '');
    return `${base}/branding/${normalize(theme)}`;
  }

  function applyTheme(root, theme) {
    const t = normalize(theme);
    root.setAttribute(ROOT_ATTR, t);
    try {
      localStorage.setItem(STORAGE_KEY, t);
    } catch (_) { /* ignore */ }

    const pathBase = document.documentElement.getAttribute('data-path-base') || '';
    const base = brandBase(pathBase, t);

    root.querySelectorAll('[data-v2-brand="logo-nav"]').forEach((img) => {
      img.setAttribute('src', `${base}/logo-dark.svg`);
    });
    root.querySelectorAll('[data-v2-brand="icon"]').forEach((img) => {
      img.setAttribute('src', `${base}/icon.svg`);
    });
    root.querySelectorAll('[data-v2-brand="icon-192"]').forEach((img) => {
      img.setAttribute('src', `${base}/icon-192.png`);
    });

    const themeColor = document.getElementById('v2-theme-color');
    if (themeColor) {
      themeColor.setAttribute('content', t === 'green' ? '#16A34A' : '#2563EB');
    }

    root.querySelectorAll('[data-v2-theme-select]').forEach((el) => {
      if (el instanceof HTMLSelectElement) el.value = t;
    });

    root.querySelectorAll('[data-v2-theme-option]').forEach((btn) => {
      const val = btn.getAttribute('data-v2-theme-option');
      btn.setAttribute('aria-selected', val === t ? 'true' : 'false');
      btn.classList.toggle('is-active', val === t);
    });
  }

  function initThemeMenu(root) {
    const menu = root.querySelector('[data-v2-theme-menu]');
    if (!menu) return;
    const toggle = menu.querySelector('[data-v2-theme-toggle]');
    const popover = menu.querySelector('[data-v2-theme-popover]');
    if (!toggle || !popover) return;

    function close() {
      popover.hidden = true;
      toggle.setAttribute('aria-expanded', 'false');
    }

    toggle.addEventListener('click', (e) => {
      e.stopPropagation();
      const open = popover.hidden;
      popover.hidden = !open;
      toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    });

    popover.querySelectorAll('[data-v2-theme-option]').forEach((btn) => {
      btn.addEventListener('click', () => {
        applyTheme(root, btn.getAttribute('data-v2-theme-option') || DEFAULT_THEME);
        close();
      });
    });

    document.addEventListener('click', (e) => {
      if (!menu.contains(e.target)) close();
    });

    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') close();
    });
  }

  function init() {
    const root = document.querySelector('.v2-app');
    if (!root) return;

    let stored = DEFAULT_THEME;
    try {
      stored = normalize(localStorage.getItem(STORAGE_KEY) || DEFAULT_THEME);
    } catch (_) {
      stored = DEFAULT_THEME;
    }
    applyTheme(root, stored);

    root.querySelectorAll('[data-v2-theme-select]').forEach((el) => {
      el.addEventListener('change', () => applyTheme(root, el.value));
    });

    initThemeMenu(root);

    const menuBtn = root.querySelector('[data-v2-menu-toggle]');
    const mobileNav = root.querySelector('[data-v2-mobile-nav]');
    if (menuBtn && mobileNav) {
      menuBtn.addEventListener('click', () => {
        const open = mobileNav.classList.toggle('is-open');
        menuBtn.setAttribute('aria-expanded', open ? 'true' : 'false');
      });
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();

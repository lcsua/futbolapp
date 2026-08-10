// MiLiga public site — theme switcher (no framework SPA)
(function () {
  const STORAGE_KEY = 'miliga-theme';
  const DEFAULT_THEME = 'blue'; // Azul Eléctrico + Azul Noche
  const THEMES = {
    green: { id: 'green', label: 'Verde + Azul Noche' },
    blue: { id: 'blue', label: 'Azul Eléctrico + Azul Noche' },
  };

  function normalize(theme) {
    return THEMES[theme] ? theme : DEFAULT_THEME;
  }

  function brandingBase() {
    const root = document.documentElement;
    const pathBase = (root.getAttribute('data-path-base') || '').replace(/\/$/, '');
    return `${pathBase}/branding`;
  }

  function applyTheme(theme, { persist = true } = {}) {
    const next = normalize(theme);
    document.documentElement.setAttribute('data-theme', next);

    if (persist) {
      try {
        localStorage.setItem(STORAGE_KEY, next);
      } catch (_) {
        /* ignore quota / private mode */
      }
    }

    const base = brandingBase();
    document.querySelectorAll('[data-brand="logo-nav"]').forEach((el) => {
      el.setAttribute('src', `${base}/${next}/logo-dark.svg`);
    });
    document.querySelectorAll('[data-brand="logo-light"]').forEach((el) => {
      el.setAttribute('src', `${base}/${next}/logo-light.svg`);
    });
    document.querySelectorAll('[data-brand="icon"]').forEach((el) => {
      el.setAttribute('src', `${base}/${next}/icon.svg`);
    });

    const favicon = document.getElementById('miliga-favicon');
    if (favicon) favicon.setAttribute('href', `${base}/${next}/favicon.ico`);
    const fav32 = document.getElementById('miliga-favicon-32');
    if (fav32) fav32.setAttribute('href', `${base}/${next}/favicon-32x32.png`);
    const fav16 = document.getElementById('miliga-favicon-16');
    if (fav16) fav16.setAttribute('href', `${base}/${next}/favicon-16x16.png`);
    const apple = document.getElementById('miliga-apple-touch');
    if (apple) apple.setAttribute('href', `${base}/${next}/apple-touch-icon.png`);

    const themeColor = document.getElementById('miliga-theme-color');
    if (themeColor) {
      themeColor.setAttribute('content', next === 'green' ? '#16A34A' : '#2563EB');
    }

    document.querySelectorAll('[data-theme-option]').forEach((btn) => {
      const isActive = btn.getAttribute('data-theme-option') === next;
      btn.classList.toggle('active', isActive);
      btn.setAttribute('aria-checked', isActive ? 'true' : 'false');
    });
  }

  function currentTheme() {
    return normalize(document.documentElement.getAttribute('data-theme'));
  }

  window.MiLigaTheme = {
    STORAGE_KEY,
    DEFAULT_THEME,
    THEMES,
    apply: applyTheme,
    current: currentTheme,
  };

  document.addEventListener('DOMContentLoaded', () => {
    let saved = DEFAULT_THEME;
    try {
      saved = normalize(localStorage.getItem(STORAGE_KEY) || DEFAULT_THEME);
    } catch (_) {
      saved = DEFAULT_THEME;
    }
    applyTheme(saved, { persist: false });

    document.querySelectorAll('[data-theme-option]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.preventDefault();
        applyTheme(btn.getAttribute('data-theme-option'));
      });
    });
  });
})();

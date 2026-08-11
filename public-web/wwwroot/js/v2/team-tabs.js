(() => {
  const VALID = new Set(['resumen', 'partidos', 'estadisticas']);

  function normalize(tab) {
    return VALID.has(tab) ? tab : 'resumen';
  }

  function readTabFromUrl() {
    try {
      return normalize(new URLSearchParams(window.location.search).get('tab') || '');
    } catch (_) {
      return 'resumen';
    }
  }

  function writeTabToUrl(tab, mode) {
    const url = new URL(window.location.href);
    const params = url.searchParams;
    if (tab === 'resumen') {
      // Keep shareable URL clean when default, but preserve if already present? User wants tab in URL.
      params.set('tab', tab);
    } else {
      params.set('tab', tab);
    }
    url.search = params.toString();
    const next = url.pathname + url.search;
    if (mode === 'replace') {
      history.replaceState({ tab }, '', next);
    } else {
      history.pushState({ tab }, '', next);
    }
  }

  function activate(root, tab, { push } = { push: false }) {
    const t = normalize(tab);
    const tabs = Array.from(root.querySelectorAll('[data-v2-tab]'));
    const panels = Array.from(root.querySelectorAll('[data-v2-panel]'));

    tabs.forEach((btn) => {
      const id = btn.getAttribute('data-v2-tab');
      const on = id === t;
      btn.classList.toggle('is-active', on);
      btn.setAttribute('aria-selected', on ? 'true' : 'false');
      btn.tabIndex = on ? 0 : -1;
    });

    panels.forEach((panel) => {
      const id = panel.getAttribute('data-v2-panel');
      const on = id === t;
      if (on) {
        panel.hidden = false;
        panel.classList.add('is-visible');
      } else {
        panel.hidden = true;
        panel.classList.remove('is-visible');
      }
    });

    const seasonTab = document.querySelector('[data-v2-season-tab]');
    if (seasonTab instanceof HTMLInputElement) seasonTab.value = t;

    if (push) writeTabToUrl(t, 'push');
  }

  function init() {
    const root = document.querySelector('[data-v2-team-tabs]');
    if (!root) return;

    const initial = normalize(
      readTabFromUrl() || root.getAttribute('data-v2-initial-tab') || 'resumen'
    );
    activate(root, initial, { push: false });
    // Sync URL if missing/invalid tab without adding history entry
    if (readTabFromUrl() !== initial) {
      writeTabToUrl(initial, 'replace');
    }

    const tabs = Array.from(root.querySelectorAll('[data-v2-tab]'));

    tabs.forEach((btn) => {
      btn.addEventListener('click', () => {
        const tab = btn.getAttribute('data-v2-tab');
        if (!tab || btn.getAttribute('aria-selected') === 'true') return;
        activate(root, tab, { push: true });
      });
    });

    root.querySelector('.v2-tabs')?.addEventListener('keydown', (e) => {
      const keys = ['ArrowLeft', 'ArrowRight', 'Home', 'End'];
      if (!keys.includes(e.key)) return;
      e.preventDefault();
      const current = tabs.findIndex((b) => b.getAttribute('aria-selected') === 'true');
      let next = current;
      if (e.key === 'ArrowLeft') next = (current - 1 + tabs.length) % tabs.length;
      if (e.key === 'ArrowRight') next = (current + 1) % tabs.length;
      if (e.key === 'Home') next = 0;
      if (e.key === 'End') next = tabs.length - 1;
      const target = tabs[next];
      if (!target) return;
      target.focus();
      activate(root, target.getAttribute('data-v2-tab'), { push: true });
    });

    window.addEventListener('popstate', () => {
      activate(root, readTabFromUrl(), { push: false });
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();

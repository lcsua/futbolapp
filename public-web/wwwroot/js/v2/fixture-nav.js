(() => {
  const root = document.querySelector('[data-v2-fixture-root]');
  if (!root) return;

  const fragmentUrl = root.getAttribute('data-fragment-url') || '';
  const pageBase = root.getAttribute('data-page-base') || '';
  let busy = false;

  function board() {
    return root.querySelector('[data-v2-fixture-board]');
  }

  function currentParams() {
    const b = board();
    const url = new URL(window.location.href);
    return {
      season: (b && b.getAttribute('data-season')) || url.searchParams.get('season') || '',
      division: (b && b.getAttribute('data-division')) || url.searchParams.get('division') || 'all',
      fecha: (b && b.getAttribute('data-fecha')) || url.searchParams.get('fecha') || ''
    };
  }

  function buildQuery(params) {
    const q = new URLSearchParams();
    if (params.season) q.set('season', params.season);
    if (params.division && params.division !== 'all') q.set('division', params.division);
    if (params.fecha) q.set('fecha', String(params.fecha));
    const s = q.toString();
    return s ? `?${s}` : '';
  }

  function bindBoard() {
    if (typeof window.v2BindTeamBadges === 'function') {
      window.v2BindTeamBadges(root);
    }

    root.querySelectorAll('[data-v2-fecha-goto]').forEach((el) => {
      el.addEventListener('click', (ev) => {
        ev.preventDefault();
        if (el.disabled) return;
        const fecha = el.getAttribute('data-v2-fecha-goto');
        if (!fecha) return;
        loadFecha(fecha, { push: true });
      });
    });
  }

  async function loadFecha(fecha, { push }) {
    if (busy || !fragmentUrl) return;
    busy = true;
    root.setAttribute('aria-busy', 'true');

    const params = currentParams();
    params.fecha = String(fecha);

    try {
      const res = await fetch(fragmentUrl + buildQuery(params), {
        headers: { Accept: 'text/html', 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin'
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const html = await res.text();
      root.innerHTML = html;
      bindBoard();

      if (push) {
        const nextUrl = pageBase + buildQuery(params);
        history.pushState({ fixtureFecha: params.fecha }, '', nextUrl);
      }
    } catch (err) {
      console.error('Fixture fecha navigation failed', err);
      window.location.href = pageBase + buildQuery(params);
    } finally {
      busy = false;
      root.removeAttribute('aria-busy');
    }
  }

  window.addEventListener('popstate', () => {
    const url = new URL(window.location.href);
    const fecha = url.searchParams.get('fecha');
    const params = currentParams();
    if (fecha && fecha !== params.fecha) {
      loadFecha(fecha, { push: false });
    } else if (!fecha && params.fecha) {
      // Reload fragment without explicit fecha → server default
      loadFecha('', { push: false });
    }
  });

  // Seed history state for the initial page so back works consistently.
  if (!history.state || !history.state.fixtureFecha) {
    const p = currentParams();
    history.replaceState({ fixtureFecha: p.fecha || null }, '', window.location.href);
  }

  bindBoard();
})();

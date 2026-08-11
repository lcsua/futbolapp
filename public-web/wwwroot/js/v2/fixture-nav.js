(() => {
  const root = document.querySelector('[data-v2-fixture-root]');
  if (!root) return;

  const fragmentUrl = root.getAttribute('data-fragment-url') || '';
  const pageBase = root.getAttribute('data-page-base') || '';
  let busy = false;

  function pageDivision() {
    const url = new URL(window.location.href);
    return root.getAttribute('data-page-division')
      || url.searchParams.get('division')
      || 'all';
  }

  function pageSeason() {
    const url = new URL(window.location.href);
    const board = root.querySelector('[data-v2-fixture-board]');
    return (board && board.getAttribute('data-season'))
      || url.searchParams.get('season')
      || '';
  }

  function buildQuery({ season, division, fecha }) {
    const q = new URLSearchParams();
    if (season) q.set('season', season);
    if (division && division !== 'all') q.set('division', division);
    if (fecha) q.set('fecha', String(fecha));
    const s = q.toString();
    return s ? `?${s}` : '';
  }

  function bind(scope) {
    if (typeof window.v2BindTeamBadges === 'function') {
      window.v2BindTeamBadges(scope || root);
    }

    (scope || root).querySelectorAll('[data-v2-fixture-block]').forEach((block) => {
      block.querySelectorAll('[data-v2-fecha-goto]').forEach((el) => {
        el.addEventListener('click', (ev) => {
          ev.preventDefault();
          if (el.disabled) return;
          const fecha = el.getAttribute('data-v2-fecha-goto');
          if (!fecha) return;
          loadBlockFecha(block, fecha, { push: true });
        });
      });
    });
  }

  async function loadBlockFecha(block, fecha, { push }) {
    if (busy || !fragmentUrl || !block) return;
    busy = true;
    block.setAttribute('aria-busy', 'true');

    const divisionSlug = block.getAttribute('data-division-slug') || '';
    const season = block.getAttribute('data-season') || pageSeason();
    const pageDiv = pageDivision();

    // Always fetch one concrete division so each block stays independent.
    const fetchParams = {
      season,
      division: divisionSlug || pageDiv,
      fecha: String(fecha)
    };

    try {
      const res = await fetch(fragmentUrl + buildQuery(fetchParams), {
        headers: { Accept: 'text/html', 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin'
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const html = await res.text();
      const wrap = document.createElement('div');
      wrap.innerHTML = html.trim();
      const next = wrap.querySelector('[data-v2-fixture-block]') || wrap.firstElementChild;
      if (!next) throw new Error('Empty fixture fragment');

      // Preserve page-level division mode on the replacement block.
      next.setAttribute('data-page-division', pageDiv);
      block.replaceWith(next);
      bind(next);

      if (push && pageDiv !== 'all') {
        const nextUrl = pageBase + buildQuery({
          season,
          division: pageDiv,
          fecha: String(fecha)
        });
        history.pushState({ fixtureFecha: String(fecha), fixtureDivision: pageDiv }, '', nextUrl);
      }
    } catch (err) {
      console.error('Fixture fecha navigation failed', err);
      const fallbackDiv = pageDiv === 'all' ? (divisionSlug || 'all') : pageDiv;
      window.location.href = pageBase + buildQuery({
        season,
        division: fallbackDiv,
        fecha: String(fecha)
      });
    } finally {
      busy = false;
    }
  }

  window.addEventListener('popstate', () => {
    if (pageDivision() === 'all') return;
    const url = new URL(window.location.href);
    const fecha = url.searchParams.get('fecha') || '';
    const block = root.querySelector('[data-v2-fixture-block]');
    if (!block) return;
    const current = block.getAttribute('data-fecha') || '';
    if (fecha !== current) {
      loadBlockFecha(block, fecha, { push: false });
    }
  });

  if (pageDivision() !== 'all') {
    const block = root.querySelector('[data-v2-fixture-block]');
    const fecha = block && block.getAttribute('data-fecha');
    history.replaceState(
      { fixtureFecha: fecha || null, fixtureDivision: pageDivision() },
      '',
      window.location.href
    );
  }

  bind(root);
})();

(() => {
  const root = document.querySelector('[data-v2-simulator]');
  if (!root || root.dataset.supported !== 'true' || !window.MiLigaStandings) return;

  const rulesNode = document.getElementById('v2-sim-rules');
  let rules = { winPoints: 3, drawPoints: 1, lossPoints: 0, tieBreakers: ['points', 'goalDifference', 'goalsFor'], adjustments: [] };
  if (rulesNode) {
    try {
      rules = JSON.parse(rulesNode.textContent || '{}');
    } catch {
      rules = rules;
    }
  }

  const leagueSlug = root.dataset.league || '';
  const season = root.dataset.season || '';
  const focusTeam = (root.dataset.focusTeam || '').toLowerCase();

  const refreshers = [];
  root.querySelectorAll('[data-sim-division][data-live="true"]').forEach((division) => {
    refreshers.push(bindDivision(division));
  });

  function renderAll() {
    refreshers.forEach((refresh) => refresh());
  }

  function bindDivision(division) {
    const matches = Array.from(division.querySelectorAll('[data-sim-match]'));
    const tbody = division.querySelector('.v2-sim-table tbody');
    const badge = division.querySelector('[data-sim-badge]');
    const count = division.querySelector('[data-sim-count]');
    const outside = division.querySelector('[data-sim-outside]');
    const reset = division.querySelector('[data-sim-reset]');
    const roundSelect = division.querySelector('[data-sim-round]');
    const board = division.querySelector('.v2-sim-board');
    if (!tbody) return function () {};

    const baseline = Array.from(tbody.querySelectorAll('tr')).map(readBaseline);
    const rowNodes = new Map();
    baseline.forEach((row) => {
      const node = tbody.querySelector(`tr[data-team-id="${cssEscape(row.teamId)}"]`);
      if (node) rowNodes.set(idKey(row.teamId), node);
    });

    let mode = 'round';

    division.querySelectorAll('[data-sim-mode]').forEach((button) => {
      button.addEventListener('click', () => {
        mode = button.dataset.simMode || 'round';
        division.querySelectorAll('[data-sim-mode]').forEach((item) => {
          item.setAttribute('aria-pressed', item === button ? 'true' : 'false');
        });
        applyRoundFilter();
        render();
      });
    });

    roundSelect?.addEventListener('change', () => {
      applyRoundFilter();
      render();
    });

    reset?.addEventListener('click', () => {
      matches.forEach((match) => {
        match.querySelectorAll('input').forEach((input) => {
          input.value = '';
          mirrorScore(match, input);
        });
      });
      renderAll();
    });

    matches.forEach((match) => {
      match.querySelectorAll('input').forEach((input) => {
        input.addEventListener('input', () => {
          mirrorScore(match, input);
          renderAll();
        });
      });
      match.querySelector('[data-sim-clear]')?.addEventListener('click', () => {
        match.querySelectorAll('input').forEach((input) => {
          input.value = '';
          mirrorScore(match, input);
        });
        renderAll();
      });
    });

    applyRoundFilter();
    render();

    function applyRoundFilter() {
      const selected = roundSelect ? roundSelect.value : '';
      division.querySelectorAll('[data-round]').forEach((block) => {
        const show = mode === 'all' || block.dataset.round === selected;
        block.hidden = !show;
      });
      if (roundSelect) roundSelect.disabled = mode === 'all';
    }

    function render() {
      const selectedRound = roundSelect ? Number(roundSelect.value) : 0;
      const scores = [];
      let outsideCount = 0;

      matches.forEach((match) => {
        const homeInput = match.querySelector('[data-side="home"]');
        const awayInput = match.querySelector('[data-side="away"]');
        const homeRaw = homeInput ? homeInput.value : '';
        const awayRaw = awayInput ? awayInput.value : '';
        const parsed = window.MiLigaStandings.tryScore(homeRaw, awayRaw);
        const touched = homeRaw.trim() !== '' || awayRaw.trim() !== '';
        const invalid = touched && !parsed;

        homeInput?.classList.toggle('is-invalid', invalid && homeRaw.trim() !== '' && window.MiLigaStandings.tryScore(homeRaw, '0') === null);
        awayInput?.classList.toggle('is-invalid', invalid && awayRaw.trim() !== '' && window.MiLigaStandings.tryScore('0', awayRaw) === null);
        homeInput?.setAttribute('aria-invalid', invalid && homeRaw.trim() !== '' ? 'true' : 'false');
        awayInput?.setAttribute('aria-invalid', invalid && awayRaw.trim() !== '' ? 'true' : 'false');

        paintOutcome(match, parsed);
        const clear = match.querySelector('[data-sim-clear]');
        if (clear) clear.hidden = !touched;

        if (!parsed) return;
        if (mode === 'round' && Number(match.dataset.round) !== selectedRound) outsideCount += 1;
        scores.push({
          homeId: match.dataset.homeId,
          homeName: match.dataset.homeName,
          homeCounts: match.dataset.homeCounts === 'true',
          awayId: match.dataset.awayId,
          awayName: match.dataset.awayName,
          awayCounts: match.dataset.awayCounts === 'true',
          homeGoals: parsed.home,
          awayGoals: parsed.away
        });
      });

      const projection = window.MiLigaStandings.projectBaseline(baseline, scores, rules);
      if (!projection.supported) {
        if (board) board.hidden = true;
        return;
      }
      if (board) board.hidden = false;

      const pending = matches.length - projection.simulated;
      if (count) {
        count.textContent = `${label(projection.simulated, 'partido simulado', 'partidos simulados')} · ${label(pending, 'partido pendiente', 'partidos pendientes')}`;
      }
      if (badge) {
        badge.textContent = projection.simulated > 0 ? 'Simulación' : 'Igual a la oficial';
        badge.classList.toggle('is-live', projection.simulated > 0);
      }
      if (reset) reset.disabled = projection.simulated === 0;
      if (outside) outside.hidden = !(mode === 'round' && outsideCount > 0);
      if (board) {
        board.setAttribute('aria-label', `Tabla proyectada. ${label(projection.simulated, 'partido simulado', 'partidos simulados')}.`);
      }

      const fragment = document.createDocumentFragment();
      projection.rows.forEach((row) => {
        const node = rowNodes.get(idKey(row.teamId)) || createRow(row);
        rowNodes.set(idKey(row.teamId), node);
        paintRow(node, row);
        fragment.appendChild(node);
      });
      tbody.replaceChildren(fragment);
    }

    function createRow(row) {
      const tr = document.createElement('tr');
      const team = sideFromScores(row.teamId);
      tr.dataset.teamId = row.teamId;
      tr.dataset.slug = team?.slug || '';
      const href = team && team.slug && leagueSlug
        ? teamHref(team.slug)
        : '';
      const name = document.createElement(href ? 'a' : 'div');
      name.className = 'v2-sim-team__link';
      if (href) name.href = href;
      name.appendChild(badgeHtml(team?.logo, team?.initials || initials(row.name), row.name));
      const labelNode = document.createElement('span');
      labelNode.className = 'v2-sim-team__name';
      labelNode.dataset.field = 'name';
      name.appendChild(labelNode);

      tr.innerHTML = `
        <td class="v2-sim-pos is-pos"><span data-field="position"></span><span class="v2-sim-move" data-field="move"></span></td>
        <td class="v2-sim-team is-team"></td>
        <td class="v2-sim-num" data-field="played"></td>
        <td class="v2-sim-num" data-field="won"></td>
        <td class="v2-sim-num" data-field="drawn"></td>
        <td class="v2-sim-num" data-field="lost"></td>
        <td class="v2-sim-num" data-field="gf"></td>
        <td class="v2-sim-num" data-field="ga"></td>
        <td class="v2-sim-num" data-field="gd"></td>
        <td class="v2-sim-pts" data-field="points"></td>
        <td class="v2-sim-delta" data-field="delta"></td>`;
      tr.querySelector('.v2-sim-team').appendChild(name);
      return tr;
    }

    return render;
  }

  function mirrorScore(match, source) {
    const id = match.dataset.matchId;
    if (!id || !source) return;
    root.querySelectorAll(`[data-match-id="${cssEscape(id)}"]`).forEach((other) => {
      if (other === match) return;
      const target = other.querySelector(`[data-side="${source.dataset.side}"]`);
      if (target && target.value !== source.value) target.value = source.value;
    });
  }

  function readBaseline(tr) {
    return {
      teamId: tr.dataset.teamId,
      name: tr.dataset.name,
      position: Number(tr.dataset.position) || 0,
      played: Number(tr.dataset.played) || 0,
      won: Number(tr.dataset.won) || 0,
      drawn: Number(tr.dataset.drawn) || 0,
      lost: Number(tr.dataset.lost) || 0,
      goalsFor: Number(tr.dataset.gf) || 0,
      goalsAgainst: Number(tr.dataset.ga) || 0,
      points: Number(tr.dataset.points) || 0
    };
  }

  function paintRow(tr, row) {
    setField(tr, 'position', String(row.position));
    setField(tr, 'name', row.name);
    setField(tr, 'played', String(row.played));
    setField(tr, 'won', String(row.won));
    setField(tr, 'drawn', String(row.drawn));
    setField(tr, 'lost', String(row.lost));
    setField(tr, 'gf', String(row.goalsFor));
    setField(tr, 'ga', String(row.goalsAgainst));
    const gd = row.goalDifference;
    setField(tr, 'gd', gd > 0 ? `+${gd}` : String(gd));
    setField(tr, 'points', String(row.points));
    const delta = tr.querySelector('[data-field="delta"]');
    if (delta) {
      delta.textContent = row.pointsDelta > 0 ? `+${row.pointsDelta}` : String(row.pointsDelta);
      delta.classList.toggle('is-plus', row.pointsDelta > 0);
      delta.classList.toggle('is-minus', row.pointsDelta < 0);
    }
    const move = tr.querySelector('[data-field="move"]');
    if (move) {
      move.className = 'v2-sim-move';
      if (!row.officialPosition) {
        move.textContent = 'Nuevo';
        move.classList.add('is-new');
      } else if (row.position < row.officialPosition) {
        move.textContent = `▲${row.officialPosition - row.position}`;
        move.classList.add('is-up');
      } else if (row.position > row.officialPosition) {
        move.textContent = `▼${row.position - row.officialPosition}`;
        move.classList.add('is-down');
      } else {
        move.textContent = '';
      }
    }
    tr.classList.toggle('is-leader', row.position === 1);
    const slug = (tr.dataset.slug || '').toLowerCase();
    tr.classList.toggle('is-focus', focusTeam !== '' && slug === focusTeam);
  }

  function paintOutcome(match, parsed) {
    const outcome = match.querySelector('[data-sim-outcome]');
    const homeSide = match.querySelector('[data-sim-side="home"]');
    const awaySide = match.querySelector('[data-sim-side="away"]');
    homeSide?.classList.remove('is-win', 'is-loss', 'is-draw');
    awaySide?.classList.remove('is-win', 'is-loss', 'is-draw');
    if (!outcome) return;
    if (!parsed) {
      outcome.hidden = true;
      outcome.textContent = '';
      outcome.className = 'v2-sim-outcome';
      return;
    }
    outcome.hidden = false;
    if (parsed.home > parsed.away) {
      outcome.textContent = 'Gana local';
      outcome.className = 'v2-sim-outcome is-home';
      homeSide?.classList.add('is-win');
      awaySide?.classList.add('is-loss');
    } else if (parsed.home < parsed.away) {
      outcome.textContent = 'Gana visita';
      outcome.className = 'v2-sim-outcome is-away';
      awaySide?.classList.add('is-win');
      homeSide?.classList.add('is-loss');
    } else {
      outcome.textContent = 'Empate';
      outcome.className = 'v2-sim-outcome is-draw';
      homeSide?.classList.add('is-draw');
      awaySide?.classList.add('is-draw');
    }
  }

  function sideFromScores(teamId) {
    const wanted = idKey(teamId);
    const match = root.querySelector(`[data-sim-match][data-home-id="${cssEscape(teamId)}"], [data-sim-match][data-away-id="${cssEscape(teamId)}"]`);
    if (!match) return null;
    if (idKey(match.dataset.homeId) === wanted) {
      return {
        slug: match.dataset.homeSlug,
        logo: match.dataset.homeLogo,
        initials: match.dataset.homeInitials
      };
    }
    return {
      slug: match.dataset.awaySlug,
      logo: match.dataset.awayLogo,
      initials: match.dataset.awayInitials
    };
  }

  function badgeHtml(logo, letters, name) {
    const wrap = document.createElement('div');
    wrap.className = `v2-team-badge v2-team-badge--sm ${logo ? 'has-logo' : 'is-fallback'}`;
    wrap.setAttribute('aria-hidden', 'true');
    wrap.title = name || '';
    if (logo) {
      const img = document.createElement('img');
      img.src = logo;
      img.alt = '';
      img.width = 28;
      img.height = 28;
      img.addEventListener('error', () => {
        wrap.classList.remove('has-logo');
        wrap.classList.add('is-fallback');
        img.remove();
      });
      wrap.appendChild(img);
    }
    const initialsNode = document.createElement('span');
    initialsNode.className = 'v2-team-badge__initials';
    initialsNode.textContent = letters || '?';
    wrap.appendChild(initialsNode);
    return wrap;
  }

  function teamHref(slug) {
    const base = document.documentElement.dataset.pathBase || '';
    const seasonQs = season ? `?season=${encodeURIComponent(season)}` : '';
    return `${base}/ligas/${encodeURIComponent(leagueSlug)}/${encodeURIComponent(slug)}${seasonQs}`;
  }

  function setField(tr, field, value) {
    const node = tr.querySelector(`[data-field="${field}"]`);
    if (node) node.textContent = value;
  }

  function label(count, singular, plural) {
    return count === 1 ? `1 ${singular}` : `${count} ${plural}`;
  }

  function initials(name) {
    const parts = String(name || '').trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }

  function idKey(id) {
    return String(id || '').toLowerCase();
  }

  function cssEscape(value) {
    if (window.CSS && CSS.escape) return CSS.escape(String(value));
    return String(value).replace(/"/g, '\\"');
  }
})();

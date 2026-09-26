(() => {
  function bindCarousel(root) {
    const track = root.querySelector('[data-v2-carousel-track]');
    if (!(track instanceof HTMLElement)) return;

    const prev = root.querySelector('[data-v2-carousel-prev]');
    const next = root.querySelector('[data-v2-carousel-next]');

    function scrollByPage(dir) {
      const amount = Math.max(track.clientWidth * 0.7, 140) * dir;
      const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      track.scrollBy({ left: amount, behavior: reduce ? 'auto' : 'smooth' });
    }

    function updateButtons() {
      const max = Math.max(0, track.scrollWidth - track.clientWidth);
      const overflowing = max > 8;
      const atStart = track.scrollLeft <= 8;
      const atEnd = track.scrollLeft >= max - 8;
      root.classList.toggle('is-overflowing', overflowing);
      root.classList.toggle('has-more-start', overflowing && !atStart);
      root.classList.toggle('has-more-end', overflowing && !atEnd);
      if (prev instanceof HTMLButtonElement) {
        prev.disabled = !overflowing || atStart;
        prev.hidden = !overflowing;
      }
      if (next instanceof HTMLButtonElement) {
        next.disabled = !overflowing || atEnd;
        next.hidden = !overflowing;
      }
    }

    function revealCurrentChip() {
      const board = root.closest('.v2-home-board');
      const checked = board?.querySelector('input[name="home-division"]:checked');
      if (!(checked instanceof HTMLInputElement) || !checked.value) return;
      const chip = track.querySelector(`[data-home-chip="${CSS.escape(checked.value)}"]`);
      if (!(chip instanceof HTMLElement)) return;
      const left = chip.offsetLeft;
      const right = left + chip.offsetWidth;
      const viewLeft = track.scrollLeft;
      const viewRight = viewLeft + track.clientWidth;
      if (left < viewLeft + 12 || right > viewRight - 12) {
        track.scrollTo({ left: Math.max(0, left - 20), behavior: 'auto' });
      }
    }

    if (prev) prev.addEventListener('click', () => scrollByPage(-1));
    if (next) next.addEventListener('click', () => scrollByPage(1));
    track.addEventListener('scroll', () => updateButtons(), { passive: true });
    window.addEventListener('resize', () => updateButtons());
    if (typeof ResizeObserver !== 'undefined') {
      new ResizeObserver(() => updateButtons()).observe(track);
    }
    revealCurrentChip();
    updateButtons();
  }

  document.querySelectorAll('[data-v2-team-carousel], [data-v2-chip-scroller]').forEach(bindCarousel);
})();

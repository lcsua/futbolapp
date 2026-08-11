(() => {
  const root = document.querySelector('[data-v2-team-carousel]');
  if (!root) return;

  const track = root.querySelector('[data-v2-carousel-track]');
  if (!(track instanceof HTMLElement)) return;

  const section = root.closest('.v2-home-section');
  const prev = section && section.querySelector('[data-v2-carousel-prev]');
  const next = section && section.querySelector('[data-v2-carousel-next]');

  function scrollByPage(dir) {
    const amount = Math.max(track.clientWidth * 0.85, 180) * dir;
    track.scrollBy({ left: amount, behavior: 'smooth' });
  }

  function updateButtons() {
    const max = track.scrollWidth - track.clientWidth - 2;
    if (prev instanceof HTMLButtonElement) {
      prev.disabled = track.scrollLeft <= 2;
    }
    if (next instanceof HTMLButtonElement) {
      next.disabled = track.scrollLeft >= max;
    }
  }

  if (prev) prev.addEventListener('click', () => scrollByPage(-1));
  if (next) next.addEventListener('click', () => scrollByPage(1));
  track.addEventListener('scroll', () => updateButtons(), { passive: true });
  window.addEventListener('resize', () => updateButtons());
  updateButtons();
})();

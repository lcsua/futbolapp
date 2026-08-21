(() => {
  const STORAGE_KEY = 'miliga-v2-standings-full';
  const page = document.querySelector('[data-v2-standings-page]');
  if (!page) return;

  const btn = page.querySelector('[data-v2-standings-expand]');
  if (!btn) return;

  const label = btn.querySelector('[data-v2-standings-expand-label]');
  const LABEL_ON = 'Ver resumen';
  const LABEL_OFF = 'Ver todas las estadísticas';

  function isFull() {
    return page.classList.contains('is-full');
  }

  function setFull(on) {
    page.classList.toggle('is-full', on);
    btn.setAttribute('aria-pressed', on ? 'true' : 'false');
    btn.setAttribute(
      'aria-label',
      on
        ? 'Mostrar solo posición, partidos, diferencia y puntos'
        : 'Mostrar todas las estadísticas de la tabla'
    );
    if (label) label.textContent = on ? LABEL_ON : LABEL_OFF;
    try {
      localStorage.setItem(STORAGE_KEY, on ? '1' : '0');
    } catch {
      /* ignore quota / private mode */
    }
  }

  let stored = false;
  try {
    stored = localStorage.getItem(STORAGE_KEY) === '1';
  } catch {
    stored = false;
  }

  setFull(stored);
  btn.addEventListener('click', () => setFull(!isFull()));
})();

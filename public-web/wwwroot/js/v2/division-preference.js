(() => {
  const league = document.documentElement.getAttribute('data-home-league') || '';
  if (!league) return;

  const lastKey = `miliga-division:${league}`;
  const favoriteKey = `miliga-favorite-division:${league}`;

  function write(key, value) {
    try {
      if (value) localStorage.setItem(key, value);
      else localStorage.removeItem(key);
    } catch {
      /* private mode */
    }
  }

  document.querySelectorAll('input[name="home-division"]').forEach((radio) => {
    radio.addEventListener('change', () => {
      if (!(radio instanceof HTMLInputElement) || !radio.checked || !radio.value) return;
      write(favoriteKey, radio.value);
      write(lastKey, radio.value);
    });
  });

  const form = document.querySelector('[data-division-preference]');
  const select = form?.querySelector('[data-division-select]');
  if (!form || form.hasAttribute('data-division-gate') || !(select instanceof HTMLSelectElement)) return;
  if (select.value) write(lastKey, select.value);
})();

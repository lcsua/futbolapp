(() => {
  function init() {
    const root = document.querySelector('.v2-app');
    if (!root) return;

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

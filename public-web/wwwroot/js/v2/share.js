(() => {
  function closeMenus(except) {
    document.querySelectorAll('[data-v2-share]').forEach((root) => {
      if (except && root === except) return;
      const menu = root.querySelector('[data-v2-share-menu]');
      const trigger = root.querySelector('[data-v2-share-trigger]');
      if (menu) menu.hidden = true;
      if (trigger) trigger.setAttribute('aria-expanded', 'false');
    });
  }

  function showToast(root) {
    const toast = root.querySelector('[data-v2-share-toast]');
    if (!toast) return;
    toast.hidden = false;
    window.clearTimeout(toast._v2ShareTimer);
    toast._v2ShareTimer = window.setTimeout(() => {
      toast.hidden = true;
    }, 1800);
  }

  function payload(trigger) {
    return {
      title: trigger.getAttribute('data-share-title') || document.title,
      text: trigger.getAttribute('data-share-text') || '',
      url: trigger.getAttribute('data-share-url') || window.location.href.split('#')[0],
    };
  }

  async function copyLink(root, url) {
    try {
      if (navigator.clipboard && navigator.clipboard.writeText) {
        await navigator.clipboard.writeText(url);
      } else {
        const input = document.createElement('input');
        input.value = url;
        input.setAttribute('readonly', '');
        input.style.position = 'absolute';
        input.style.left = '-9999px';
        document.body.appendChild(input);
        input.select();
        document.execCommand('copy');
        document.body.removeChild(input);
      }
      showToast(root);
    } catch {
      window.prompt('Copiá el enlace:', url);
    }
  }

  function openWhatsApp({ text, url }) {
    const message = [text, url].filter(Boolean).join('\n');
    const href = `https://wa.me/?text=${encodeURIComponent(message)}`;
    window.open(href, '_blank', 'noopener,noreferrer');
  }

  function toggleMenu(root, open) {
    const menu = root.querySelector('[data-v2-share-menu]');
    const trigger = root.querySelector('[data-v2-share-trigger]');
    if (!menu || !trigger) return;
    const next = typeof open === 'boolean' ? open : menu.hidden;
    if (next) closeMenus(root);
    menu.hidden = !next;
    trigger.setAttribute('aria-expanded', next ? 'true' : 'false');
  }

  async function onTriggerClick(root, trigger) {
    const data = payload(trigger);
    if (typeof navigator.share === 'function') {
      try {
        await navigator.share(data);
        closeMenus();
        return;
      } catch (err) {
        if (err && err.name === 'AbortError') return;
      }
    }
    toggleMenu(root);
  }

  function bind(root) {
    if (!(root instanceof HTMLElement) || root.dataset.v2ShareBound) return;
    root.dataset.v2ShareBound = '1';
    const trigger = root.querySelector('[data-v2-share-trigger]');
    if (!(trigger instanceof HTMLButtonElement)) return;

    trigger.addEventListener('click', (e) => {
      e.preventDefault();
      e.stopPropagation();
      void onTriggerClick(root, trigger);
    });

    const wa = root.querySelector('[data-v2-share-whatsapp]');
    if (wa) {
      wa.addEventListener('click', (e) => {
        e.preventDefault();
        openWhatsApp(payload(trigger));
        toggleMenu(root, false);
      });
    }

    const copy = root.querySelector('[data-v2-share-copy]');
    if (copy) {
      copy.addEventListener('click', (e) => {
        e.preventDefault();
        void copyLink(root, payload(trigger).url).then(() => toggleMenu(root, false));
      });
    }
  }

  function init(scope) {
    (scope || document).querySelectorAll('[data-v2-share]').forEach(bind);
  }

  document.addEventListener('click', () => closeMenus());
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') closeMenus();
  });

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => init());
  } else {
    init();
  }

  window.v2BindShareButtons = init;
})();

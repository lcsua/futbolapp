(() => {
  const pathBase = (document.documentElement.getAttribute('data-path-base') || '').replace(/\/$/, '');
  // API lives on the host apex (/api/...), independent of PublicWeb PathBase.
  const API_BASE = '/api/public/push';
  const SW_URL = `${pathBase}/sw.js` || '/sw.js';
  const SW_SCOPE = pathBase ? `${pathBase}/` : '/';

  function supported() {
    return 'serviceWorker' in navigator
      && 'PushManager' in window
      && 'Notification' in window;
  }

  function track(name, params) {
    try {
      if (typeof gtag === 'function') gtag('event', name, params || {});
    } catch (_) { /* ignore */ }
  }

  function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = atob(base64);
    const out = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; i++) out[i] = raw.charCodeAt(i);
    return out;
  }

  async function getPublicKey() {
    const res = await fetch(`${API_BASE}/vapid-public-key`, { credentials: 'omit' });
    if (!res.ok) throw new Error('Web Push no configurado');
    const data = await res.json();
    return data.publicKey;
  }

  async function ensureSubscription() {
    const reg = await navigator.serviceWorker.register(SW_URL || '/sw.js', { scope: SW_SCOPE });
    await navigator.serviceWorker.ready;
    let sub = await reg.pushManager.getSubscription();
    if (!sub) {
      const key = await getPublicKey();
      sub = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(key)
      });
    }
    const json = sub.toJSON();
    await fetch(`${API_BASE}/subscribe`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify({
        endpoint: json.endpoint,
        p256dh: json.keys && json.keys.p256dh,
        auth: json.keys && json.keys.auth
      })
    });
    return json;
  }

  async function getStatus(endpoint, scopeType, scopeId) {
    const qs = new URLSearchParams({ endpoint, scopeType, scopeId });
    const res = await fetch(`${API_BASE}/status?${qs}`, { credentials: 'omit' });
    if (!res.ok) return false;
    const data = await res.json();
    return !!data.following;
  }

  function setUi(root, state, message) {
    const btn = root.querySelector('[data-v2-follow-trigger]');
    const labelEl = root.querySelector('[data-v2-follow-label]');
    const hint = root.querySelector('[data-v2-follow-hint]');
    const label = root.getAttribute('data-label') || 'Seguir';
    const followingLabel = root.getAttribute('data-following-label') || 'Siguiendo';
    root.dataset.state = state;
    if (!btn || !labelEl) return;

    btn.disabled = state === 'loading' || state === 'unsupported';
    if (state === 'following') {
      btn.classList.add('is-following');
      btn.setAttribute('aria-pressed', 'true');
      btn.setAttribute('aria-label', followingLabel);
      labelEl.textContent = followingLabel;
    } else if (state === 'loading') {
      btn.classList.remove('is-following');
      btn.setAttribute('aria-pressed', 'false');
      labelEl.textContent = 'Activando…';
    } else {
      btn.classList.remove('is-following');
      btn.setAttribute('aria-pressed', 'false');
      btn.setAttribute('aria-label', label);
      labelEl.textContent = label;
    }

    if (hint) {
      if (message) {
        hint.hidden = false;
        hint.textContent = message;
      } else {
        hint.hidden = true;
        hint.textContent = '';
      }
    }

    root.dispatchEvent(new CustomEvent('v2-follow-state', { bubbles: true, detail: { state } }));
  }

  async function refresh(root) {
    try {
      const reg = await navigator.serviceWorker.getRegistration('/');
      const sub = reg && await reg.pushManager.getSubscription();
      if (!sub) {
        setUi(root, 'not-following');
        return;
      }
      const following = await getStatus(
        sub.endpoint,
        root.getAttribute('data-scope-type'),
        root.getAttribute('data-scope-id')
      );
      setUi(root, following ? 'following' : 'not-following');
    } catch (_) {
      setUi(root, 'not-following');
    }
  }

  async function onClick(root) {
    if (root.dataset.state === 'loading') return;
    const scopeType = root.getAttribute('data-scope-type');
    const scopeId = root.getAttribute('data-scope-id');
    const isFollowing = root.dataset.state === 'following';

    if (Notification.permission === 'denied') {
      setUi(root, 'permission-denied', 'Las notificaciones están bloqueadas en tu navegador. Podés habilitarlas desde la configuración del sitio.');
      track('push_permission_denied', { scope_type: scopeType });
      return;
    }

    setUi(root, 'loading');
    try {
      if (isFollowing) {
        const reg = await navigator.serviceWorker.ready;
        const sub = await reg.pushManager.getSubscription();
        if (sub) {
          await fetch(`${API_BASE}/follow`, {
            method: 'DELETE',
            headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
            body: JSON.stringify({ endpoint: sub.endpoint, scopeType, scopeId })
          });
        }
        setUi(root, 'not-following');
        track(scopeType === 'Team' ? 'unfollow_team' : 'unfollow_league', { scope_type: scopeType });
        return;
      }

      const permission = await Notification.requestPermission();
      if (permission !== 'granted') {
        track('push_permission_denied', { scope_type: scopeType });
        setUi(root, 'permission-denied', 'Las notificaciones están bloqueadas en tu navegador. Podés habilitarlas desde la configuración del sitio.');
        return;
      }
      track('push_permission_granted', { scope_type: scopeType });

      const json = await ensureSubscription();
      const res = await fetch(`${API_BASE}/follow`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify({
          endpoint: json.endpoint,
          p256dh: json.keys && json.keys.p256dh,
          auth: json.keys && json.keys.auth,
          scopeType,
          scopeId
        })
      });
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw new Error(err.message || 'No se pudo activar el seguimiento');
      }
      setUi(root, 'following');
      track(scopeType === 'Team' ? 'follow_team' : 'follow_league', { scope_type: scopeType });
    } catch (err) {
      setUi(root, 'error', 'No se pudo activar el seguimiento. Probá de nuevo.');
    }
  }

  function bind(root) {
    if (!(root instanceof HTMLElement) || root.dataset.bound) return;
    root.dataset.bound = '1';
    if (!supported()) return;
    root.hidden = false;
    const btn = root.querySelector('[data-v2-follow-trigger]');
    if (btn) btn.addEventListener('click', (e) => { e.preventDefault(); void onClick(root); });
    void refresh(root);
  }

  function init(scope) {
    (scope || document).querySelectorAll('[data-v2-follow]').forEach(bind);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => init());
  } else {
    init();
  }

  window.v2BindFollowButtons = init;
})();

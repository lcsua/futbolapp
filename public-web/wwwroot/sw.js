/* global self, clients */

const HOME_PATHS = new Set(['', '/', '/public-web', '/public-web/']);
const LEAGUE_PATH_RE = /^(argentina\/)?[a-z0-9]+(?:-[a-z0-9]+)*$/;

function cookieValue(header, name) {
  if (!header) return '';
  const prefix = `${name}=`;
  const parts = header.split(';');
  for (const part of parts) {
    const trimmed = part.trim();
    if (trimmed.startsWith(prefix)) {
      try {
        return decodeURIComponent(trimmed.slice(prefix.length));
      } catch {
        return trimmed.slice(prefix.length);
      }
    }
  }
  return '';
}

self.addEventListener('fetch', (event) => {
  if (event.request.mode !== 'navigate' || event.request.method !== 'GET') return;

  const url = new URL(event.request.url);
  if (url.origin !== self.location.origin) return;
  if (!HOME_PATHS.has(url.pathname)) return;
  if (url.searchParams.has('inicio') || url.searchParams.has('todas')) return;

  const header = event.request.headers.get('Cookie') || '';
  const target = cookieValue(header, 'miliga-home-league') || cookieValue(header, 'miliga-last-league');
  if (!LEAGUE_PATH_RE.test(target)) return;

  event.respondWith(Response.redirect(new URL(`/ligas/${target}`, url.origin), 302));
});

self.addEventListener('push', (event) => {
  let data = {};
  try {
    data = event.data ? event.data.json() : {};
  } catch (_) {
    data = { title: 'Mi Liga', body: event.data ? event.data.text() : '' };
  }

  const title = data.title || 'Mi Liga';
  const options = {
    body: data.body || '',
    icon: data.icon || '/branding/blue/icon-192.png',
    badge: data.badge || '/branding/blue/icon-192.png',
    data: { url: data.url || '/' },
    vibrate: [80, 40, 80]
  };

  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const targetUrl = (event.notification.data && event.notification.data.url) || '/';
  const absolute = new URL(targetUrl, self.location.origin).href;

  event.waitUntil((async () => {
    const all = await clients.matchAll({ type: 'window', includeUncontrolled: true });
    for (const client of all) {
      if ('focus' in client) {
        await client.focus();
        if ('navigate' in client) {
          try { await client.navigate(absolute); } catch (_) { /* ignore */ }
        }
        return;
      }
    }
    if (clients.openWindow) {
      await clients.openWindow(absolute);
    }
  })());
});

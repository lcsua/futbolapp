/* global self, clients */

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

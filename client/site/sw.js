// Service worker of the tracking page: shows the Web Push alerts ("nearly your turn", "your turn") on the lock screen, page open or not.
self.addEventListener('install', () => self.skipWaiting())
self.addEventListener('activate', (e) => e.waitUntil(self.clients.claim()))

self.addEventListener('push', (e) => {
  let d = {}
  try { d = e.data ? e.data.json() : {} } catch { d = { body: e.data ? e.data.text() : '' } }
  e.waitUntil(self.registration.showNotification(d.title || 'Qdee', {
    body: d.body || '',
    icon: '/favicon-192.png',
    badge: '/favicon-192.png',
    tag: d.tag || 'qdee', // a newer alert of the same kind replaces the older one
    renotify: true, // ...and still makes the sound again
    requireInteraction: !!d.urgent, // "your turn" stays until it is touched
    vibrate: d.urgent ? [400, 150, 400, 150, 800] : [250, 120, 250],
    data: { url: d.url || '/' }
  }))
})

self.addEventListener('notificationclick', (e) => {
  e.notification.close()
  const url = (e.notification.data && e.notification.data.url) || '/'
  e.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((list) => {
    for (const c of list) if ('focus' in c) return c.focus()
    return self.clients.openWindow(url)
  }))
})

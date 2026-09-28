// Ada-ncoa — service worker.
// Strategy:
//  - static assets (css/js/icons, CDN libraries): cache-first;
//  - page navigations: network-first, falling back to the offline page;
//  - dynamic donation data and uploaded images are NOT cached aggressively.
const VERSION = 'v9';
const STATIC_CACHE = `dmd-static-${VERSION}`;
const OFFLINE_URL = '/offline.html';

const PRECACHE_URLS = [
    OFFLINE_URL,
    '/css/site.css',
    '/js/site.js',
    '/js/donation-wizard.js',
    '/manifest.webmanifest',
    '/images/icons/icon-192.png',
    '/images/icons/icon-512.png',
    '/images/icons/favicon-32.png'
];

const CDN_HOSTS = ['cdn.jsdelivr.net'];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(STATIC_CACHE)
            .then((cache) => cache.addAll(PRECACHE_URLS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys()
            .then((keys) => Promise.all(
                keys.filter((key) => key.startsWith('dmd-') && key !== STATIC_CACHE)
                    .map((key) => caches.delete(key))))
            .then(() => self.clients.claim())
    );
});

function isStaticAsset(url) {
    if (CDN_HOSTS.includes(url.hostname)) {
        return true;
    }
    if (url.origin !== self.location.origin) {
        return false;
    }
    return url.pathname.startsWith('/css/')
        || url.pathname.startsWith('/js/')
        || url.pathname.startsWith('/images/')
        || url.pathname === '/manifest.webmanifest'
        || url.pathname === '/favicon.ico';
}

self.addEventListener('fetch', (event) => {
    const request = event.request;
    if (request.method !== 'GET') {
        return; // Never intercept form posts.
    }

    const url = new URL(request.url);

    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request).catch(() => caches.match(OFFLINE_URL))
        );
        return;
    }

    if (isStaticAsset(url)) {
        event.respondWith(
            caches.match(request, { ignoreSearch: url.origin === self.location.origin }).then((cached) => {
                if (cached) {
                    return cached;
                }
                return fetch(request).then((response) => {
                    if (response && (response.ok || response.type === 'opaque')) {
                        const copy = response.clone();
                        caches.open(STATIC_CACHE).then((cache) => cache.put(request, copy));
                    }
                    return response;
                });
            })
        );
    }
    // Everything else (pages data, uploads) goes straight to the network.
});

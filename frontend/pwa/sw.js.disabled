const CACHE_VERSION = "tinker-genie-v1757280849";
const STATIC_CACHE = CACHE_VERSION + "-static";
const API_CACHE = CACHE_VERSION + "-api";

const STATIC_FILES = [
    "/",
    "/index.html",
    "/manifest.json",
    "/icon-192.png",
    "/icon-512.png"
];

// Install event - cache static resources
self.addEventListener("install", event => {
    console.log("Service Worker installing with version:", CACHE_VERSION);
    
    event.waitUntil(
        caches.open(STATIC_CACHE)
            .then(cache => cache.addAll(STATIC_FILES))
            .then(() => self.skipWaiting())
    );
});

// Activate event - clean up old caches
self.addEventListener("activate", event => {
    console.log("Service Worker activating");
    
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cacheName => {
                    if (!cacheName.includes(CACHE_VERSION)) {
                        console.log("Deleting old cache:", cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        }).then(() => self.clients.claim())
    );
});

// Fetch event - serve from cache or network
self.addEventListener("fetch", event => {
    const url = new URL(event.request.url);
    
    // Handle API requests
    if (url.pathname.startsWith("/api/")) {
        event.respondWith(
            caches.open(API_CACHE).then(cache => {
                return fetch(event.request).then(response => {
                    // Only cache successful GET requests
                    if (event.request.method === "GET" && response.status === 200) {
                        cache.put(event.request, response.clone());
                    }
                    return response;
                }).catch(() => {
                    // Return cached version if network fails
                    return cache.match(event.request);
                });
            })
        );
        return;
    }
    
    // Handle static files
    event.respondWith(
        caches.match(event.request).then(response => {
            return response || fetch(event.request);
        })
    );
});

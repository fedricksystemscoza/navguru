// ============================================================
// NavGuru Offline Cache Helper
// Client-side caching for offline-first behaviour.
// Storage: localStorage (per-origin, ~5MB quota).
// ============================================================

(function (window) {
    'use strict';

    const CACHE_PREFIX = 'navguru-cache-';
    const MAX_AGE_MS = 7 * 24 * 60 * 60 * 1000;   // 7 days

    // ---------- Core read/write ----------
    function write(key, value) {
        try {
            const payload = {
                cachedAt: Date.now(),
                version: 1,
                data: value
            };
            localStorage.setItem(CACHE_PREFIX + key, JSON.stringify(payload));
            return true;
        } catch (e) {
            console.warn('[NavGuru] Cache write failed for', key, e.message);
            return false;
        }
    }

    function read(key) {
        try {
            const raw = localStorage.getItem(CACHE_PREFIX + key);
            if (!raw) return null;

            const payload = JSON.parse(raw);
            if (!payload || typeof payload.cachedAt !== 'number') return null;

            // Expiry
            if (Date.now() - payload.cachedAt > MAX_AGE_MS) {
                localStorage.removeItem(CACHE_PREFIX + key);
                return null;
            }

            return payload;
        } catch (e) {
            console.warn('[NavGuru] Cache read failed for', key, e.message);
            return null;
        }
    }

    function remove(key) {
        localStorage.removeItem(CACHE_PREFIX + key);
    }

    function clearAll() {
        Object.keys(localStorage)
            .filter(k => k.startsWith(CACHE_PREFIX))
            .forEach(k => localStorage.removeItem(k));
    }

    // ---------- Time helpers ----------
    function timeAgo(timestamp) {
        const seconds = Math.floor((Date.now() - timestamp) / 1000);
        if (seconds < 60) return 'just now';
        const mins = Math.floor(seconds / 60);
        if (mins < 60) return mins + ' min ago';
        const hrs = Math.floor(mins / 60);
        if (hrs < 24) return hrs + ' hr ago';
        const days = Math.floor(hrs / 24);
        return days + ' day' + (days === 1 ? '' : 's') + ' ago';
    }

    // ---------- Online/offline events ----------
    function onStatusChange(callback) {
        window.addEventListener('online', () => callback(true));
        window.addEventListener('offline', () => callback(false));
        callback(navigator.onLine);
    }

    // ---------- Public API ----------
    window.NavGuruCache = {
        write,
        read,
        remove,
        clearAll,
        timeAgo,
        onStatusChange,
        isOnline: () => navigator.onLine
    };

    console.log('[NavGuru] Offline cache helper loaded');
})(window);
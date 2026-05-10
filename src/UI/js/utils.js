/* ===== Utility Functions ===== */

function parseTags(tagsStr) {
    try { return JSON.parse(tagsStr || '[]'); } catch { return []; }
}

function formatDate(isoStr) {
    if (!isoStr) return '';
    const d = new Date(isoStr);
    return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

function escapeHTML(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

function escapeAttr(str) {
    return str.replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/'/g, '&#39;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function formatPlayTime(totalSeconds) {
    if (!totalSeconds || totalSeconds <= 0) return '—';
    const h = Math.floor(totalSeconds / 3600);
    const m = Math.floor((totalSeconds % 3600) / 60);
    if (h > 0) return `${h}h ${m}m`;
    return `${m}m`;
}

function safeHttpUrl(url) {
    if (!url) return false;
    try {
        const parsed = new URL(url);
        return parsed.protocol === 'http:' || parsed.protocol === 'https:';
    } catch {
        return false;
    }
}

function delegate(root, selector, eventName, handler) {
    if (!root) return;
    root.addEventListener(eventName, (e) => {
        const match = e.target.closest(selector);
        if (match && root.contains(match)) handler(e, match);
    });
}

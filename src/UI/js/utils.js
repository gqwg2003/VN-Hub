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

function relativeTime(isoStr) {
    if (!isoStr) return '';
    const diff = Date.now() - new Date(isoStr).getTime();
    const days = Math.floor(diff / 86400000);
    if (days === 0) return t('relativeToday') || 'Today';
    if (days === 1) return t('relativeYesterday') || 'Yesterday';
    if (days < 7)  return days + (t('relativeDaysAgo') || 'd ago');
    if (days < 30) return Math.floor(days / 7) + (t('relativeWeeksAgo') || 'w ago');
    return Math.floor(days / 30) + (t('relativeMonthsAgo') || 'mo ago');
}

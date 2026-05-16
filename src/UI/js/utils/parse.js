function parseTags(tagsStr) {
    try { return JSON.parse(tagsStr || '[]'); } catch { return []; }
}

function parseLinks(linksJson) {
    try { return JSON.parse(linksJson || '[]'); } catch { return []; }
}

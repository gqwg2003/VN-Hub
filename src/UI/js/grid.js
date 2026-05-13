function highlightText(text, query) {
    if (!query) return escapeHTML(text);
    const escaped = escapeHTML(text);
    const terms = query.split(/\s+/).filter(Boolean).map(t => t.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'));
    if (terms.length === 0) return escaped;
    const re = new RegExp(`(${terms.join('|')})`, 'gi');
    return escaped.replace(re, '<mark>$1</mark>');
}

function updateSearchResultCount() {
    let badge = document.getElementById('searchResultCount');
    if (!badge) {
        badge = document.createElement('span');
        badge.id = 'searchResultCount';
        badge.className = 'search-result-count';
        const topbar = document.querySelector('.topbar-search');
        if (topbar) topbar.appendChild(badge);
    }
    if (state.searchQuery) {
        const count = state.entries.length;
        badge.textContent = (t('searchResults') || '{0} results').replace('{0}', count);
        badge.style.display = '';
    } else {
        badge.style.display = 'none';
    }
}

function showSkeletonGrid() {
    const grid = document.getElementById('cardGrid');
    if (!grid) return;
    grid.className = 'card-grid grid-' + state.gridSize;
    const count = 12;
    let html = '';
    for (let i = 0; i < count; i++) {
        html += `<div class="vn-card skeleton-card">
            <div class="skeleton-cover shimmer"></div>
            <div class="vn-card-info">
                <div class="skeleton-title shimmer"></div>
                <div class="skeleton-meta shimmer"></div>
            </div>
        </div>`;
    }
    grid.innerHTML = html;
}

const VIRTUAL = {
    allItems: [],
    rendered: new Set(),
    observer: null,
    batchSize: 40,
    sentinel: null
};

function renderGrid() {
    const grid = document.getElementById('cardGrid');
    const empty = document.getElementById('emptyState');

    const filtered = state.entries;

    if (filtered.length === 0) {
        grid.innerHTML = '';
        VIRTUAL.allItems = [];
        VIRTUAL.rendered.clear();
        renderEmptyState(empty);
        empty.style.display = '';
        renderReadingStrip([]);
        return;
    }

    empty.style.display = 'none';
    renderReadingStrip(filtered);

    grid.className = 'card-grid grid-' + state.gridSize;

    VIRTUAL.allItems = filtered;
    VIRTUAL.rendered.clear();
    grid.innerHTML = '';

    if (VIRTUAL.sentinel) {
        VIRTUAL.sentinel.remove();
        VIRTUAL.sentinel = null;
    }
    if (VIRTUAL.observer) {
        VIRTUAL.observer.disconnect();
    }

    appendBatch(grid, 0, VIRTUAL.batchSize);

    if (VIRTUAL.allItems.length > VIRTUAL.batchSize) {
        VIRTUAL.sentinel = document.createElement('div');
        VIRTUAL.sentinel.className = 'grid-sentinel';
        VIRTUAL.sentinel.style.height = '1px';
        grid.after(VIRTUAL.sentinel);

        VIRTUAL.observer = new IntersectionObserver((entries) => {
            if (entries[0].isIntersecting && VIRTUAL.rendered.size < VIRTUAL.allItems.length) {
                const start = VIRTUAL.rendered.size;
                appendBatch(grid, start, start + VIRTUAL.batchSize);
                if (VIRTUAL.rendered.size >= VIRTUAL.allItems.length) {
                    VIRTUAL.observer.disconnect();
                    VIRTUAL.sentinel.remove();
                }
            }
        }, { rootMargin: '200px' });

        VIRTUAL.observer.observe(VIRTUAL.sentinel);
    }
}

function appendBatch(grid, start, end) {
    const items = VIRTUAL.allItems;
    const realEnd = Math.min(end, items.length);
    const fragment = document.createDocumentFragment();

    for (let i = start; i < realEnd; i++) {
        if (VIRTUAL.rendered.has(i)) continue;
        VIRTUAL.rendered.add(i);

        const wrapper = document.createElement('div');
        wrapper.innerHTML = cardHTML(items[i]);
        const card = wrapper.firstElementChild;
        fragment.appendChild(card);
    }

    grid.appendChild(fragment);

    grid.querySelectorAll('.vn-card:not([data-bound])').forEach(card => {
        bindCardEvents(card);
    });
}

function renderEmptyState(el) {
    let title = t('emptyTitle');
    let hint = t('emptyHint');
    const svgBook = '<svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1 0-5H20"/></svg>';

    if (state.activeTag) {
        title = t('emptyTagTitle');
        hint = t('emptyTagHint');
    } else {
        switch (state.activeTab) {
            case 'marked':
                if (state.markedSubTab === 'favorites') {
                    title = t('emptyFavTitle');
                    hint = t('emptyFavHint');
                } else if (state.markedSubTab === 'priority') {
                    title = t('emptyPriorityTitle');
                    hint = t('emptyPriorityHint');
                } else if (state.markedSubTab === 'completed') {
                    title = t('emptyCompletedTitle');
                    hint = t('emptyCompletedHint');
                } else {
                    title = t('emptyMarkedTitle');
                    hint = t('emptyMarkedHint');
                }
                break;
            case 'reading':
                title = t('emptyReadingTitle');
                hint = t('emptyReadingHint');
                break;
        }
        if (state.activeGroupId) {
            title = t('emptyGroupTitle');
            hint = t('emptyGroupHint');
        }
    }

    el.innerHTML = `${svgBook}<h3>${escapeHTML(title)}</h3><p>${escapeHTML(hint)}</p>`;
}

function clearTagFilter() {
    state.activeTag = null;
    const badge = document.getElementById('activeTagBadge');
    if (badge) badge.style.display = 'none';
    refreshLibrary();
}

function setTagFilter(tag) {
    state.activeTag = tag;
    refreshLibrary();

    let badge = document.getElementById('activeTagBadge');
    if (!badge) {
        badge = document.createElement('span');
        badge.id = 'activeTagBadge';
        badge.className = 'active-tag-badge';
        const topbar = document.querySelector('.topbar-search');
        if (topbar) topbar.appendChild(badge);
    }
    badge.style.display = '';
    badge.innerHTML = `${escapeHTML(tag)} <span class="active-tag-clear">&times;</span>`;
    badge.querySelector('.active-tag-clear').addEventListener('click', (e) => {
        e.stopPropagation();
        clearTagFilter();
    });

    const dd = document.getElementById('tagDropdown');
    if (dd) dd.classList.remove('open');
}

function getAllTags() {
    if (Array.isArray(state._tagsCache)) return state._tagsCache;
    return [];
}

function toggleTagDropdown() {
    const dd = document.getElementById('tagDropdown');
    if (!dd) return;

    if (dd.classList.contains('open')) {
        dd.classList.remove('open');
        return;
    }

    if (!Array.isArray(state._tagsCache)) {
        send('getTags');
        dd.innerHTML = `<div class="tag-dropdown-empty">${escapeHTML(t('loading') || '...')}</div>`;
        dd.classList.add('open');
        return;
    }

    renderTagDropdown(dd);
}

function renderTagDropdown(dd) {
    dd = dd || document.getElementById('tagDropdown');
    if (!dd) return;
    const tags = getAllTags();
    if (tags.length === 0) {
        dd.innerHTML = `<div class="tag-dropdown-empty">${escapeHTML(t('noTags'))}</div>`;
    } else {
        dd.innerHTML = tags.map(tag =>
            `<button class="tag-dropdown-item${state.activeTag && state.activeTag.toLowerCase() === tag.name.toLowerCase() ? ' active' : ''}" data-tag="${escapeAttr(tag.name)}">
                <span class="tag-dropdown-name">${escapeHTML(tag.name)}</span>
                <span class="tag-dropdown-count">${tag.count}</span>
            </button>`
        ).join('');

        dd.querySelectorAll('.tag-dropdown-item').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const tag = btn.dataset.tag;
                if (state.activeTag && state.activeTag.toLowerCase() === tag.toLowerCase()) {
                    clearTagFilter();
                    dd.classList.remove('open');
                } else {
                    setTagFilter(tag);
                }
            });
        });
    }

    dd.classList.add('open');

    const closeHandler = (e) => {
        if (!dd.contains(e.target) && e.target.id !== 'btnTagDropdown' && !e.target.closest('#btnTagDropdown')) {
            dd.classList.remove('open');
            document.removeEventListener('click', closeHandler);
        }
    };
    setTimeout(() => document.addEventListener('click', closeHandler), 0);
}

function cardHTML(entry) {
    const isRunning = state.runningGames.has(entry.id);
    const statusLabels = getStatusLabels();
    const coverSrc = entry.coverPath
        ? `https://covers.vnhub.local/${entry.coverPath}`
        : '';

    const coverEl = coverSrc
        ? `<img class="vn-card-cover" src="${escapeAttr(coverSrc)}?t=${entry.coverPath}" alt="" loading="lazy">`
        : `<div class="vn-card-cover-placeholder">
            <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="9" cy="9" r="2"/><path d="m21 15-3.086-3.086a2 2 0 0 0-2.828 0L6 21"/></svg>
           </div>`;

    const icons = [];
    if (isRunning) icons.push('<span class="vn-card-icon running"><svg viewBox="0 0 24 24" fill="currentColor" stroke="none"><polygon points="5 3 19 12 5 21 5 3"/></svg></span>');
    if (entry.isFavorite) icons.push('<span class="vn-card-icon heart"><svg viewBox="0 0 24 24" fill="currentColor" stroke="none"><path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"/></svg></span>');
    if (entry.isPinned) icons.push('<span class="vn-card-icon star"><svg viewBox="0 0 24 24" fill="currentColor" stroke="none"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg></span>');

    const playSvg = isRunning
        ? `<svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" stroke="none"><rect x="6" y="4" width="4" height="16"/><rect x="14" y="4" width="4" height="16"/></svg>`
        : `<svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" stroke="none"><polygon points="5 3 19 12 5 21 5 3"/></svg>`;

    const playBtn = entry.exePath
        ? `<button class="vn-card-play${isRunning ? ' is-running' : ''}" data-id="${escapeAttr(entry.id)}" title="${isRunning ? t('running') : t('launch')}">
            ${playSvg}
           </button>`
        : '';

    const playTimeStr = entry.playTimeSeconds > 0
        ? `<span class="vn-card-playtime">${formatPlayTime(entry.playTimeSeconds)}</span>`
        : '';

    const lastPlayedStr = state.activeTab === 'reading' && entry.lastLaunchedAt
        ? `<span class="vn-card-lastplayed">${relativeTime(entry.lastLaunchedAt)}</span>`
        : '';

    const vndbBadge = entry.vndbId
        ? '<span class="vn-card-vndb" title="VNDB">V</span>'
        : '';

    const progressBar = entry.readingProgress > 0
        ? `<div class="vn-card-progress"><div class="vn-card-progress-fill" style="width:${entry.readingProgress}%"></div></div>`
        : '';

    return `
    <div class="vn-card${isRunning ? ' is-running' : ''}" data-id="${escapeAttr(entry.id)}">
        ${icons.length ? `<div class="vn-card-icons">${icons.join('')}</div>` : ''}
        ${coverEl}
        ${playBtn}
        <div class="vn-card-info">
            <div class="vn-card-title" title="${escapeAttr(entry.title)}">${highlightText(entry.title, state.searchQuery)}</div>
            <div class="vn-card-meta">
                <span class="status-badge" data-status="${entry.status}">${statusLabels[entry.status] || ''}</span>
                ${playTimeStr}
                ${lastPlayedStr}
                ${vndbBadge}
            </div>
        </div>
        ${progressBar}
    </div>`;
}

function renderReadingStrip(entries) {
    const strip = document.getElementById('readingStrip');
    if (!strip) return;
    if (state.activeTab !== 'reading' || entries.length === 0) {
        strip.style.display = 'none';
        return;
    }

    const totalSec = entries.reduce((s, e) => s + (e.playTimeSeconds || 0), 0);
    const withProgress = entries.filter(e => e.readingProgress > 0);
    const avgProgress = withProgress.length > 0
        ? Math.round(withProgress.reduce((s, e) => s + e.readingProgress, 0) / withProgress.length)
        : 0;

    const svgBook = '<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/></svg>';
    const svgClock = '<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>';
    const svgTarget = '<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><circle cx="12" cy="12" r="6"/><circle cx="12" cy="12" r="2"/></svg>';

    strip.innerHTML = `
        <span class="reading-strip-stat">${svgBook} <strong>${entries.length}</strong> ${t('readingStripInProgress') || 'in progress'}</span>
        ${totalSec > 0 ? `<span class="reading-strip-stat">${svgClock} <strong>${formatPlayTime(totalSec)}</strong> ${t('readingStripTotal') || 'total'}</span>` : ''}
        ${avgProgress > 0 ? `<span class="reading-strip-stat">${svgTarget} ${t('readingStripAvg') || 'Avg.'} <strong>${avgProgress}%</strong></span>` : ''}
    `;
    strip.style.display = 'flex';
}

function patchCard(entry) {
    const grid = document.getElementById('cardGrid');
    if (!grid) return;
    const oldCard = grid.querySelector(`.vn-card[data-id="${CSS.escape(entry.id)}"]`);
    if (!oldCard) {
        renderGrid();
        return;
    }
    const wrapper = document.createElement('div');
    wrapper.innerHTML = cardHTML(entry);
    const newCard = wrapper.firstElementChild;
    oldCard.replaceWith(newCard);
    bindCardEvents(newCard);
}

function patchRunningCards(ids) {
    const grid = document.getElementById('cardGrid');
    if (!grid) return;
    for (const id of ids) {
        const entry = state.entries.find(e => e.id === id);
        if (!entry) continue;
        const card = grid.querySelector(`.vn-card[data-id="${CSS.escape(id)}"]`);
        if (!card) continue;

        const wrapper = document.createElement('div');
        wrapper.innerHTML = cardHTML(entry);
        const newCard = wrapper.firstElementChild;
        card.replaceWith(newCard);
        bindCardEvents(newCard);
    }
}

function bindCardEvents(card) {
    card.setAttribute('data-bound', '1');

    const playBtn = card.querySelector('.vn-card-play');
    if (playBtn) {
        playBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            send('launchVn', { id: playBtn.dataset.id });
        });
    }

    card.addEventListener('click', (e) => {
        const id = card.dataset.id;
        if (e.ctrlKey || e.metaKey) {
            e.preventDefault();
            toggleSelect(id);
            return;
        }
        if (state.selectedIds && state.selectedIds.size > 0) {
            toggleSelect(id);
            return;
        }
        const entry = state.entries.find(en => en.id === id);
        if (entry) openDetail(entry);
    });

    card.addEventListener('contextmenu', (e) => {
        e.preventDefault();
        const id = card.dataset.id;
        const entry = state.entries.find(en => en.id === id);
        if (entry) showContextMenu(e.clientX, e.clientY, entry);
    });
}

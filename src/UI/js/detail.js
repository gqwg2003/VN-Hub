function initDetail() {
    document.getElementById('detailClose').addEventListener('click', closeDetail);
    document.getElementById('detailOverlay').addEventListener('click', closeDetail);

    initDetailHeader();
    initDetailFields();
    initTagEditor();
    initLinkEditor();
    initStarRating();
    initDetailActions();
}

function initDetailHeader() {
    document.getElementById('detailFav').addEventListener('click', () => {
        if (state.detailEntry) send('toggleFavorite', { id: state.detailEntry.id });
    });

    document.getElementById('detailPin').addEventListener('click', () => {
        if (state.detailEntry) send('togglePin', { id: state.detailEntry.id });
    });

    document.getElementById('detailStatus').addEventListener('change', (e) => {
        if (state.detailEntry) send('setStatus', { id: state.detailEntry.id, status: parseInt(e.target.value) });
    });

    document.getElementById('btnLaunch').addEventListener('click', () => {
        if (state.detailEntry) send('launchVn', { id: state.detailEntry.id });
    });

    document.getElementById('btnBrowseExe').addEventListener('click', () => send('pickExe'));
    document.getElementById('btnChangeCover').addEventListener('click', () => send('pickImage'));
    document.getElementById('btnFetchVndb').addEventListener('click', () => {
        if (state.detailEntry) {
            send('fetchVndb', { id: state.detailEntry.id });
            showToast(t('vndbSearching'), 'info');
        }
    });
    document.getElementById('btnOpenVnFolder').addEventListener('click', () => {
        if (state.detailEntry) send('openFolder', { id: state.detailEntry.id });
    });
}

function initDetailFields() {
    document.getElementById('detailTitle').addEventListener('input', () => debouncedSaveDetail());
    document.getElementById('detailTitle').addEventListener('change', () => saveDetail());

    let notesTimer;
    document.getElementById('detailNotes').addEventListener('input', () => {
        clearTimeout(notesTimer);
        notesTimer = setTimeout(() => debouncedSaveDetail(), 800);
    });

    document.getElementById('detailProgress').addEventListener('input', (e) => {
        document.getElementById('detailProgressValue').textContent = e.target.value + '%';
    });
    document.getElementById('detailProgress').addEventListener('change', (e) => {
        if (state.detailEntry) {
            state.detailEntry.readingProgress = parseInt(e.target.value);
            saveDetail();
        }
    });

    document.getElementById('detailSkipVndb').addEventListener('change', (e) => {
        if (state.detailEntry) {
            state.detailEntry.skipVndb = e.target.checked;
            saveDetail();
        }
    });
}

function initTagEditor() {
    document.getElementById('detailTagInput').addEventListener('keydown', (e) => {
        if (e.key !== 'Enter') return;
        e.preventDefault();
        const input = e.target;
        const tag = input.value.trim();
        if (tag && state.detailEntry) {
            const tags = parseTags(state.detailEntry.tags);
            if (!tags.includes(tag)) {
                tags.push(tag);
                state.detailEntry.tags = JSON.stringify(tags);
                saveDetail();
                renderDetail();
            }
        }
        input.value = '';
    });
}

function initLinkEditor() {
    document.getElementById('btnAddLink').addEventListener('click', () => {
        const labelInput = document.getElementById('detailLinkLabel');
        const urlInput = document.getElementById('detailLinkUrl');
        const label = labelInput.value.trim();
        const url = urlInput.value.trim();
        if (!url || !state.detailEntry) return;
        if (!safeHttpUrl(url)) {
            showToast(t('invalidUrl') || 'Only http/https URLs are allowed.', 'error');
            return;
        }
        const links = parseLinks(state.detailEntry.links);
        links.push({ label: label || url, url });
        state.detailEntry.links = JSON.stringify(links);
        labelInput.value = '';
        urlInput.value = '';
        saveDetail();
        renderLinks();
    });

    document.getElementById('detailLinkUrl').addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            document.getElementById('btnAddLink').click();
        }
    });
}

function initStarRating() {
    document.querySelectorAll('#starRating .star').forEach(star => {
        star.addEventListener('click', () => {
            if (!state.detailEntry) return;
            state.detailEntry.userRating = parseInt(star.dataset.value);
            renderStars();
            saveDetail();
        });
    });
    document.getElementById('starClear').addEventListener('click', () => {
        if (!state.detailEntry) return;
        state.detailEntry.userRating = null;
        renderStars();
        saveDetail();
    });

    document.querySelectorAll('.cat-stars').forEach(group => {
        group.querySelectorAll('.cat-star').forEach(star => {
            star.addEventListener('click', () => {
                if (!state.detailEntry) return;
                const field = group.dataset.field;
                const val = parseInt(star.dataset.value);
                state.detailEntry[field] = state.detailEntry[field] === val ? null : val;
                renderCategoryRatings();
                saveDetail();
            });
        });
    });
}

function initDetailActions() {
    document.getElementById('btnDeleteVn').addEventListener('click', () => {
        document.getElementById('deleteModal').style.display = '';
    });
    document.getElementById('btnDeleteCancel').addEventListener('click', closeDeleteModal);
    document.getElementById('btnDeleteConfirm').addEventListener('click', () => {
        if (state.detailEntry) {
            send('deleteVn', { id: state.detailEntry.id });
            closeDeleteModal();
        }
    });

    document.getElementById('btnShowSessions').addEventListener('click', () => {
        const list = document.getElementById('sessionList');
        if (list.style.display === 'none' && state.detailEntry) {
            send('getSessions', { id: state.detailEntry.id });
            list.style.display = '';
        } else {
            list.style.display = 'none';
        }
    });
}

function openDetail(entry) {
    state.detailEntry = { ...entry };
    renderDetail();
    document.getElementById('detailPanel').classList.add('open');
    document.getElementById('detailOverlay').classList.add('open');
}

function closeDetail() {
    document.getElementById('detailPanel').classList.remove('open');
    document.getElementById('detailOverlay').classList.remove('open');
    state.detailEntry = null;
}

function renderDetail() {
    const e = state.detailEntry;
    if (!e) return;

    const coverImg = document.getElementById('detailCover');
    if (e.coverPath) {
        coverImg.src = `https://covers.vnhub.local/${e.coverPath}?t=${Date.now()}`;
        coverImg.style.display = '';
    } else {
        coverImg.src = '';
        coverImg.style.display = 'none';
    }

    document.getElementById('detailTitle').value = e.title;
    document.getElementById('detailStatus').value = e.status;
    document.getElementById('detailExe').value = e.exePath || '';
    document.getElementById('detailNotes').value = e.notes || '';
    document.getElementById('detailDate').textContent = formatDate(e.dateAdded);
    document.getElementById('detailPlayTime').textContent = formatPlayTime(e.playTimeSeconds);
    document.getElementById('detailLastLaunched').textContent = e.lastLaunchedAt ? formatDate(e.lastLaunchedAt) : '—';

    const completedWrap = document.getElementById('detailCompletedWrap');
    const completedEl = document.getElementById('detailCompleted');
    if (e.completedAt) {
        completedEl.textContent = formatDate(e.completedAt);
        completedWrap.style.display = '';
    } else {
        completedWrap.style.display = 'none';
    }
    const descWrap = document.getElementById('detailDescWrap');
    const descEl = document.getElementById('detailDesc');
    if (e.description) {
        descEl.textContent = e.description;
        descWrap.style.display = '';
    } else {
        descWrap.style.display = 'none';
    }

    const ratingWrap = document.getElementById('detailRatingWrap');
    const ratingEl = document.getElementById('detailRating');
    if (e.rating) {
        ratingEl.textContent = (e.rating / 10).toFixed(1) + ' / 10';
        ratingWrap.style.display = '';
    } else {
        ratingWrap.style.display = 'none';
    }

    const vndbBtn = document.getElementById('btnFetchVndb');
    if (vndbBtn) vndbBtn.style.display = (state.settings.vndbEnabled !== false) ? '' : 'none';

    const skipVndbCheck = document.getElementById('detailSkipVndb');
    const skipVndbWrap = document.getElementById('detailSkipVndbWrap');
    if (skipVndbCheck) skipVndbCheck.checked = e.skipVndb === true;
    if (skipVndbWrap) skipVndbWrap.style.display = (state.settings.vndbEnabled !== false) ? '' : 'none';

    const favBtn = document.getElementById('detailFav');
    const pinBtn = document.getElementById('detailPin');
    favBtn.classList.toggle('active-heart', e.isFavorite);
    pinBtn.classList.toggle('active-star', e.isPinned);

    renderTags();
    renderLinks();
    renderStars();
    renderCategoryRatings();
    updateLaunchButton();

    const progressSlider = document.getElementById('detailProgress');
    const progressValue = document.getElementById('detailProgressValue');
    if (progressSlider && progressValue) {
        const pv = e.readingProgress || 0;
        progressSlider.value = pv;
        progressValue.textContent = pv + '%';
    }
}

function updateLaunchButton() {
    const e = state.detailEntry;
    if (!e) return;
    const isRunning = state.runningGames.has(e.id);
    const playIcon = document.querySelector('.launch-icon-play');
    const pauseIcon = document.querySelector('.launch-icon-pause');
    const text = document.getElementById('btnLaunchText');
    const btn = document.getElementById('btnLaunch');
    if (playIcon) playIcon.style.display = isRunning ? 'none' : '';
    if (pauseIcon) pauseIcon.style.display = isRunning ? '' : 'none';
    if (text) text.textContent = isRunning ? t('running') : t('launch');
    if (btn) btn.classList.toggle('is-running', isRunning);
}

function renderTags() {
    const container = document.getElementById('tagsContainer');
    const tags = parseTags(state.detailEntry?.tags);
    container.innerHTML = tags.map((tag, i) =>
        `<span class="tag-chip"><span class="tag-chip-text" data-tag="${escapeAttr(tag)}">${escapeHTML(tag)}</span><span class="tag-chip-remove" data-idx="${i}">&times;</span></span>`
    ).join('');

    container.querySelectorAll('.tag-chip-remove').forEach(btn => {
        btn.addEventListener('click', (ev) => {
            ev.stopPropagation();
            const idx = parseInt(btn.dataset.idx);
            const tags = parseTags(state.detailEntry?.tags);
            tags.splice(idx, 1);
            state.detailEntry.tags = JSON.stringify(tags);
            saveDetail();
            renderTags();
        });
    });

    container.querySelectorAll('.tag-chip-text').forEach(el => {
        el.addEventListener('click', (ev) => {
            ev.stopPropagation();
            const tag = el.dataset.tag;
            if (tag) {
                closeDetail();
                setTagFilter(tag);
            }
        });
    });
}

function renderLinks() {
    const container = document.getElementById('linksContainer');
    if (!container) return;
    const links = parseLinks(state.detailEntry?.links);
    if (links.length === 0) {
        container.innerHTML = '';
        return;
    }
    container.innerHTML = links.map((link, i) =>
        `<div class="link-chip">
            <svg class="link-chip-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg>
            <a class="link-chip-text" href="${escapeAttr(link.url)}" target="_blank" title="${escapeAttr(link.url)}">${escapeHTML(link.label || link.url)}</a>
            <span class="link-chip-remove" data-idx="${i}">&times;</span>
        </div>`
    ).join('');

    container.querySelectorAll('.link-chip-remove').forEach(btn => {
        btn.addEventListener('click', (ev) => {
            ev.stopPropagation();
            const idx = parseInt(btn.dataset.idx);
            const links = parseLinks(state.detailEntry?.links);
            links.splice(idx, 1);
            state.detailEntry.links = JSON.stringify(links);
            saveDetail();
            renderLinks();
        });
    });

    container.querySelectorAll('.link-chip-text').forEach(a => {
        a.addEventListener('click', (ev) => {
            ev.preventDefault();
            ev.stopPropagation();
            send('openUrl', { url: a.href });
        });
    });
}

function renderStars() {
    const rating = state.detailEntry?.userRating || 0;
    document.querySelectorAll('#starRating .star').forEach(star => {
        const val = parseInt(star.dataset.value);
        star.classList.toggle('active', val <= rating);
    });
    const clearBtn = document.getElementById('starClear');
    if (clearBtn) clearBtn.style.display = rating > 0 ? '' : 'none';
}

function renderCategoryRatings() {
    const e = state.detailEntry;
    if (!e) return;
    document.querySelectorAll('.cat-stars').forEach(group => {
        const field = group.dataset.field;
        const rating = e[field] || 0;
        group.querySelectorAll('.cat-star').forEach(star => {
            const val = parseInt(star.dataset.value);
            star.classList.toggle('active', val <= rating);
        });
    });
}

let _saveDetailTimer = null;

function debouncedSaveDetail() {
    clearTimeout(_saveDetailTimer);
    _saveDetailTimer = setTimeout(() => saveDetail(), 500);
}

function saveDetail() {
    clearTimeout(_saveDetailTimer);
    const e = state.detailEntry;
    if (!e) return;
    e.title = document.getElementById('detailTitle').value.trim() || e.title;
    e.notes = document.getElementById('detailNotes').value;
    send('updateVn', e);
}

function closeDeleteModal() {
    document.getElementById('deleteModal').style.display = 'none';
}

function renderSessionHistory(sessions) {
    const list = document.getElementById('sessionList');
    if (!sessions || sessions.length === 0) {
        list.innerHTML = `<p class="settings-hint">${t('noSessions')}</p>`;
        return;
    }
    list.innerHTML = sessions.map(s => {
        const date = new Date(s.startedAt).toLocaleString();
        return `<div class="session-item">
            <span>${date}</span>
            <span>${formatPlayTime(s.seconds)}</span>
        </div>`;
    }).join('');
}

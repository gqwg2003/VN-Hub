/* ===== Statistics ===== */

function initStatsTabs() {
    const tabs = document.querySelectorAll('.stats-tab');
    if (!tabs.length || tabs[0].dataset.bound) return;
    tabs.forEach(tab => {
        tab.dataset.bound = '1';
        tab.addEventListener('click', () => {
            document.querySelectorAll('.stats-tab').forEach(t => t.classList.remove('active'));
            document.querySelectorAll('.stats-tab-content').forEach(c => c.classList.remove('active'));
            tab.classList.add('active');
            const content = document.querySelector(`.stats-tab-content[data-tab-content="${tab.dataset.tab}"]`);
            if (content) content.classList.add('active');
        });
    });
}

function renderStats(data) {
    const statusLabels = getStatusLabels();
    const statusColors = ['var(--status-reading)', 'var(--status-completed)', 'var(--status-onhold)', 'var(--status-dropped)', 'var(--status-plan)'];

    /* ── Summary cards ── */
    const icons = {
        library: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></svg>',
        clock: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>',
        check: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>',
        book: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/></svg>',
        heart: '<svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="none"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/></svg>',
        star: '<svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor" stroke="none"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>'
    };
    const cardsEl = document.getElementById('statsCards');
    cardsEl.innerHTML = `
        <div class="stat-card stat-card-accent">
            <div class="stat-card-icon">${icons.library}</div>
            <div class="stat-card-body">
                <div class="stat-card-value">${data.totalVn}</div>
                <div class="stat-card-label">${t('statsTotalVn')}</div>
            </div>
        </div>
        <div class="stat-card stat-card-accent">
            <div class="stat-card-icon">${icons.clock}</div>
            <div class="stat-card-body">
                <div class="stat-card-value">${formatPlayTime(data.totalPlayTime)}</div>
                <div class="stat-card-label">${t('statsTotalPlayTime')}</div>
            </div>
        </div>
        <div class="stat-card">
            <div class="stat-card-icon">${icons.check}</div>
            <div class="stat-card-body">
                <div class="stat-card-value">${data.byStatus?.[1] || 0}</div>
                <div class="stat-card-label">${t('statusCompleted')}</div>
            </div>
        </div>
        <div class="stat-card">
            <div class="stat-card-icon">${icons.book}</div>
            <div class="stat-card-body">
                <div class="stat-card-value">${data.byStatus?.[0] || 0}</div>
                <div class="stat-card-label">${t('statusReading')}</div>
            </div>
        </div>
        <div class="stat-card">
            <div class="stat-card-icon">${icons.heart}</div>
            <div class="stat-card-body">
                <div class="stat-card-value">${data.favCount || 0}</div>
                <div class="stat-card-label">${t('favorites')}</div>
            </div>
        </div>
        <div class="stat-card">
            <div class="stat-card-icon">${icons.star}</div>
            <div class="stat-card-body">
                <div class="stat-card-value">${data.avgRating || '—'}</div>
                <div class="stat-card-label">${t('statsAvgRating')}</div>
            </div>
        </div>
    `;

    renderRanking('statsTopPlayed', data.topPlayed, 'playTime');

    renderRanking('statsTopRated', data.topRated, 'userRating');

    const barsEl = document.getElementById('statsStatusBars');
    const totalByStatus = Object.values(data.byStatus || {}).reduce((s, v) => s + v, 0);
    if (totalByStatus === 0) {
        barsEl.innerHTML = `<div class="stats-empty-state"><svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg><p>${t('statsNoStatus')}</p></div>`;
    } else {
        const maxCount = Math.max(...Object.values(data.byStatus || {}), 1);
        let barsHTML = '';
        for (let i = 0; i <= 4; i++) {
            const count = data.byStatus?.[i] || 0;
            const pct = (count / maxCount * 100).toFixed(1);
            barsHTML += `
            <div class="stats-bar-row">
                <span class="stats-bar-label">${statusLabels[i]}</span>
                <div class="stats-bar-track">
                    <div class="stats-bar-fill" style="width: ${pct}%; background: ${statusColors[i]};"></div>
                </div>
                <span class="stats-bar-count">${count}</span>
            </div>`;
        }
        barsEl.innerHTML = barsHTML;
    }

    const chartEl = document.getElementById('statsChart');
    const months = data.monthlyAdds || {};
    const sortedMonths = Object.keys(months).sort();
    if (sortedMonths.length === 0) {
        chartEl.innerHTML = `<div class="stats-empty-state"><svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18"/><path d="M9 21V9"/></svg><p>${t('statsNoMonthly')}</p></div>`;
    } else {
        const maxMonth = Math.max(...Object.values(months), 1);
        let chartHTML = '<div class="stats-chart-bars">';
        for (const m of sortedMonths) {
            const h = (months[m] / maxMonth * 100).toFixed(1);
            chartHTML += `
            <div class="stats-chart-col">
                <div class="stats-chart-bar" style="height: ${h}%;" title="${months[m]}"></div>
                <span class="stats-chart-label">${m.slice(2)}</span>
            </div>`;
        }
        chartHTML += '</div>';
        chartEl.innerHTML = chartHTML;
    }

    renderAchievements(data);
    initStatsTabs();
}

function renderRanking(elId, items, mode) {
    const el = document.getElementById(elId);
    if (!el) return;
    if (!items || items.length === 0) {
        el.innerHTML = `<div class="stats-empty-state"><svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M12 20V10"/><path d="M18 20V4"/><path d="M6 20v-4"/></svg><p>${t('statsNoRanking')}</p></div>`;
        return;
    }
    el.innerHTML = items.map((item, i) => {
        const rank = i + 1;
        const medalClass = rank <= 3 ? ` rank-${rank}` : '';
        const coverSrc = item.cover
            ? `https://covers.vnhub.local/${encodeURIComponent(item.cover)}`
            : '';
        const coverEl = coverSrc
            ? `<img class="ranking-cover" src="${coverSrc}" alt="" loading="lazy">`
            : `<div class="ranking-cover ranking-no-cover">?</div>`;
        const value = mode === 'userRating'
            ? `${'★'.repeat(item.userRating || 0)}${'☆'.repeat(10 - (item.userRating || 0))}`
            : formatPlayTime(item.playTime);
        const sub = mode === 'userRating'
            ? `<span class="ranking-sub">${formatPlayTime(item.playTime)}</span>`
            : (item.userRating ? `<span class="ranking-sub">${'★'.repeat(item.userRating)}</span>` : '');

        return `<div class="ranking-item${medalClass}" data-id="${escapeAttr(item.id)}">
            <span class="ranking-pos">${rank}</span>
            ${coverEl}
            <div class="ranking-info">
                <span class="ranking-title">${escapeHTML(item.title)}</span>
                <span class="ranking-value">${value}</span>
                ${sub}
            </div>
        </div>`;
    }).join('');

    el.querySelectorAll('.ranking-item').forEach(row => {
        row.addEventListener('click', () => {
            const id = row.dataset.id;
            const entry = state.entries.find(e => e.id === id);
            if (entry) openDetail(entry);
        });
    });
}

function renderPlayStats(data) {
    state._playStatsDays = data.days || 30;
    renderPlayTimeChart('statsPlayChart', data.data, data.days || 30);
    initPlayStatsPeriod();
}

function initPlayStatsPeriod() {
    const container = document.getElementById('playStatsPeriod');
    if (!container || container.dataset.bound) return;
    container.dataset.bound = '1';
    container.addEventListener('click', (e) => {
        const btn = e.target.closest('.stats-period-btn');
        if (!btn) return;
        container.querySelectorAll('.stats-period-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        const days = parseInt(btn.dataset.days);
        send('getPlayStats', { days });
    });
}

function renderPlayTimeChart(elId, dayData, days) {
    const el = document.getElementById(elId);
    if (!el) return;
    if (!dayData || dayData.length === 0) {
        el.innerHTML = `<div class="stats-empty-state"><svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="M3 9h18"/><path d="M9 21V9"/></svg><p>${t('statsNoPlayData')}</p></div>`;
        return;
    }
    const map = {};
    dayData.forEach(d => { map[d.day] = d.seconds || d.totalSeconds || 0; });

    let labels;
    if (days <= 0) {
        labels = Object.keys(map).sort();
    } else {
        labels = [];
        const now = new Date();
        for (let i = days - 1; i >= 0; i--) {
            const d = new Date(now);
            d.setDate(d.getDate() - i);
            labels.push(d.toISOString().slice(0, 10));
        }
    }

    const maxVal = Math.max(...labels.map(l => map[l] || 0), 1);

    const showEvery = labels.length > 60 ? Math.ceil(labels.length / 30) :
                       labels.length > 30 ? 2 : 1;

    let html = '<div class="stats-chart-bars">';
    for (let i = 0; i < labels.length; i++) {
        const label = labels[i];
        const sec = map[label] || 0;
        const h = (sec / maxVal * 100).toFixed(1);
        const shortLabel = label.slice(5);
        const title = formatPlayTime(sec);
        const showLabel = i % showEvery === 0;
        html += `
        <div class="stats-chart-col">
            <div class="stats-chart-bar" style="height: ${h}%;" title="${title}"></div>
            <span class="stats-chart-label">${showLabel ? shortLabel : ''}</span>
        </div>`;
    }
    html += '</div>';
    el.innerHTML = html;
}

/* ===== Achievements ===== */

function renderAchievements(data) {
    const el = document.getElementById('statsAchievements');
    if (!el) return;

    const achIcons = {
        book: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/></svg>',
        books: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/><path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/></svg>',
        library: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 9l4-4 4 4"/><path d="M7 5v14"/><path d="M11 18l4 4 4-4"/><path d="M15 3v16"/><rect x="19" y="4" width="2" height="16" rx="1"/></svg>',
        archive: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="3" width="20" height="5" rx="1"/><path d="M4 8v11a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8"/><path d="M10 12h4"/></svg>',
        check: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>',
        award: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="8" r="6"/><path d="M15.477 12.89 17 22l-5-3-5 3 1.523-9.11"/></svg>',
        crown: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M2 4l3 12h14l3-12-6 7-4-7-4 7-6-7z"/><path d="M5 16h14v4H5z"/></svg>',
        clock: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>',
        flame: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M8.5 14.5A2.5 2.5 0 0 0 11 12c0-1.38-.5-2-1-3-1.072-2.143-.224-4.054 2-6 .5 2.5 2 4.9 4 6.5 2 1.6 3 3.5 3 5.5a7 7 0 1 1-14 0c0-1.153.433-2.294 1-3a2.5 2.5 0 0 0 2.5 2.5z"/></svg>',
        gem: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M6 3h12l4 6-10 13L2 9z"/><path d="M11 3l1 10"/><path d="M2 9h20"/><path d="M6.5 3 12 13"/><path d="M17.5 3 12 13"/></svg>',
        heart: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"/></svg>',
        star: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>',
    };

    const iconMap = {
        achFirstVn: achIcons.book,
        ach10Vn: achIcons.books,
        ach50Vn: achIcons.library,
        ach100Vn: achIcons.archive,
        achFirstComplete: achIcons.check,
        ach10Complete: achIcons.award,
        ach25Complete: achIcons.crown,
        ach10Hours: achIcons.clock,
        ach100Hours: achIcons.flame,
        ach500Hours: achIcons.gem,
        achFirstFav: achIcons.heart,
        achFirstRating: achIcons.star,
    };

    const list = Array.isArray(data.achievements) ? data.achievements : [];
    const newlyUnlocked = Array.isArray(data.newlyUnlocked) ? data.newlyUnlocked : [];

    for (const key of newlyUnlocked) {
        showToast(t('achievementUnlocked') + ' ' + t(key), 'success');
    }

    el.innerHTML = list.map(a => {
        const icon = iconMap[a.key] || achIcons.star;
        const dateStr = a.unlockedAt ? new Date(a.unlockedAt).toLocaleDateString() : '';
        return `
        <div class="achievement${a.unlocked ? ' unlocked' : ''}">
            <span class="achievement-icon">${icon}</span>
            <div class="achievement-info">
                <span class="achievement-title">${t(a.key)}</span>
                <span class="achievement-desc">${t(a.key + 'Desc')}</span>
                ${dateStr ? `<span class="achievement-date">${dateStr}</span>` : ''}
            </div>
        </div>
    `}).join('');
}

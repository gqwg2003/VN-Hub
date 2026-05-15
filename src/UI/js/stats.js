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

    const icons = { library: ICONS.book, clock: ICONS.clock, check: ICONS.check, book: ICONS.books, heart: ICONS.heart, star: ICONS.star, target: ICONS.target };
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
        <div class="stat-card">
            <div class="stat-card-icon">${icons.target}</div>
            <div class="stat-card-body">
                <div class="stat-card-value">${data.avgCompletionTime > 0 ? formatPlayTime(data.avgCompletionTime) : '—'}</div>
                <div class="stat-card-label">${t('statsAvgCompletion')}</div>
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
    renderRatingDistribution(data.byRating);
    renderTopTags(data.topTags);
    renderCategoryRatings(data.categoryAvgs);
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

function renderAchievements(data) {
    const el = document.getElementById('statsAchievements');
    if (!el) return;

    const svg = ICONS;

    const iconMap = {
        achFirstVn:       svg.book,
        ach10Vn:          svg.books,
        ach50Vn:          svg.library,
        ach100Vn:         svg.archive,
        achFirstComplete: svg.check,
        ach10Complete:    svg.award,
        ach25Complete:    svg.crown,
        ach10Hours:       svg.clock,
        ach100Hours:      svg.flame,
        ach500Hours:      svg.gem,
        achFirstFav:      svg.heart,
        achFirstRating:   svg.star,
        tagMilf5:         svg.moon,
        tagMilf10:        svg.moon,
        tagMilf25:        svg.moon,
        tagHarem5:        svg.users,
        tagHarem10:       svg.users,
        tagHarem25:       svg.users,
        tagYuri5:         svg.flower,
        tagYuri25:        svg.flower,
        tagSchool5:       svg.school,
        tagSchool25:      svg.school,
        tagFantasy5:      svg.sword,
        tagFantasy25:     svg.sword,
        tagTsundere5:     svg.zap,
        tagTsundere10:    svg.zap,
        tagMoege5:        svg.star,
        tagMoege25:       svg.star,
        tagRomance5:      svg.heart,
        tagRomance25:     svg.heart,
        tagIncest5:       svg.drama,
        tagIncest10:      svg.drama,
        tagIncest25:      svg.drama,
        tagAhegao5:       svg.eye,
        tagAhegao25:      svg.eye,
        tagExhibit5:      svg.eye,
        tagExhibit25:     svg.eye,
        tagCreampie5:     svg.drop,
        tagCreampie25:    svg.drop,
        tagPregnancy5:    svg.baby,
        tagPregnancy25:   svg.baby,
    };

    const list = Array.isArray(data.achievements) ? data.achievements : [];
    const newlyUnlocked = Array.isArray(data.newlyUnlocked) ? data.newlyUnlocked : [];

    for (const key of newlyUnlocked) {
        showToast(t('achievementUnlocked') + ' ' + t(key), 'success');
    }

    el.innerHTML = list.map(a => {
        const icon = iconMap[a.key] || svg.star;
        const dateStr = a.unlockedAt ? new Date(a.unlockedAt).toLocaleDateString() : '';
        const flavorText = a.unlocked ? escapeHTML(t(a.key + 'Flavor')) : '???';
        const flavorClass = a.unlocked ? 'achievement-flavor' : 'achievement-flavor achievement-flavor-locked';
        return `
        <div class="achievement${a.unlocked ? ' unlocked' : ''}" data-key="${escapeAttr(a.key)}">
            <div class="achievement-header">
                <span class="achievement-icon">${icon}</span>
                <div class="achievement-info">
                    <span class="achievement-title">${escapeHTML(t(a.key))}</span>
                    <span class="achievement-desc">${escapeHTML(t(a.key + 'Desc'))}</span>
                    ${dateStr ? `<span class="achievement-date">${dateStr}</span>` : ''}
                </div>
                <button class="achievement-expand-btn" aria-label="expand">${svg.chevron}</button>
            </div>
            <div class="achievement-body">
                <div class="achievement-body-inner">
                    <div class="achievement-flavor-block">
                        <div class="achievement-section-label">${escapeHTML(t('achFlavorLabel'))}</div>
                        <div class="${flavorClass}">${flavorText}</div>
                    </div>
                    <div class="achievement-info-block">
                        <div class="achievement-section-label">${escapeHTML(t('achInfoLabel'))}</div>
                        <div class="achievement-real-info">${escapeHTML(t(a.key + 'Info'))}</div>
                    </div>
                </div>
            </div>
        </div>
    `}).join('');

    el.querySelectorAll('.achievement').forEach(card => {
        card.addEventListener('click', () => {
            const body = card.querySelector('.achievement-body');
            const expanded = card.classList.toggle('expanded');
            body.style.maxHeight = expanded ? body.scrollHeight + 'px' : '0';
        });
    });
}

function renderRatingDistribution(byRating) {
    const el = document.getElementById('statsRatingDist');
    if (!el) return;
    const total = Object.values(byRating || {}).reduce((s, v) => s + v, 0);
    if (total === 0) {
        el.innerHTML = `<div class="stats-empty-state"><svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg><p>${t('statsNoRatingData')}</p></div>`;
        return;
    }
    const maxCount = Math.max(...Object.values(byRating), 1);
    let html = '';
    for (let r = 10; r >= 1; r--) {
        const count = byRating[r] || 0;
        const pct = (count / maxCount * 100).toFixed(1);
        html += `
        <div class="stats-bar-row">
            <span class="stats-bar-label">${r}</span>
            <div class="stats-bar-track">
                <div class="stats-bar-fill" style="width: ${pct}%; background: var(--accent);"></div>
            </div>
            <span class="stats-bar-count">${count}</span>
        </div>`;
    }
    el.innerHTML = html;
}

function renderTopTags(topTags) {
    const el = document.getElementById('statsTopTags');
    if (!el) return;
    if (!topTags || topTags.length === 0) {
        el.innerHTML = `<div class="stats-empty-state"><svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/><line x1="7" y1="7" x2="7.01" y2="7"/></svg><p>${t('statsNoTagData')}</p></div>`;
        return;
    }
    const maxCount = topTags[0].count || 1;
    el.innerHTML = topTags.map(({ tag, count }) => {
        const pct = (count / maxCount * 100).toFixed(1);
        return `
        <div class="stats-bar-row">
            <span class="stats-bar-label">${escapeHTML(tag)}</span>
            <div class="stats-bar-track">
                <div class="stats-bar-fill" style="width: ${pct}%; background: var(--accent);"></div>
            </div>
            <span class="stats-bar-count">${count}</span>
        </div>`;
    }).join('');
}

function renderCategoryRatings(categoryAvgs) {
    const el = document.getElementById('statsCategoryRatings');
    if (!el) return;
    const cats = [
        { label: t('storyRating'),     val: categoryAvgs?.story },
        { label: t('artRating'),       val: categoryAvgs?.art },
        { label: t('musicRating'),     val: categoryAvgs?.music },
        { label: t('characterRating'), val: categoryAvgs?.character },
    ];
    if (!cats.some(c => c.val != null)) {
        el.innerHTML = `<div class="stats-empty-state"><svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg><p>${t('statsNoCategoryData')}</p></div>`;
        return;
    }
    const colors = ['var(--status-reading)', 'var(--status-completed)', 'var(--status-onhold)', 'var(--status-dropped)'];
    el.innerHTML = cats.map(({ label, val }, i) => {
        const pct = val != null ? (val / 10 * 100).toFixed(1) : 0;
        const display = val != null ? val.toFixed(1) : '—';
        return `
        <div class="stats-bar-row">
            <span class="stats-bar-label">${label}</span>
            <div class="stats-bar-track">
                <div class="stats-bar-fill" style="width: ${pct}%; background: ${colors[i]};"></div>
            </div>
            <span class="stats-bar-count">${display}</span>
        </div>`;
    }).join('');
}

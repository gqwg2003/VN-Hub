/* ===== VN-Hub Init ===== */

async function loadPartials() {
    const partials = [
        { id: 'detailPanel', src: 'partials/detail-panel.html' },
        { id: 'contentStatistics', src: 'partials/statistics.html' },
        { id: 'contentSettings', src: 'partials/settings.html' }
    ];
    await Promise.all(partials.map(async ({ id, src }) => {
        const resp = await fetch(src);
        const html = await resp.text();
        document.getElementById(id).innerHTML = html;
    }));
}

document.addEventListener('DOMContentLoaded', async () => {
    await loadPartials();
    initNavigation();
    initSearch();
    initAddModal();
    initDetail();
    initSettings();
    initMarkedTabs();
    initSortAndGrid();
    initMultiSelect();
    initGroups();
    initTagDropdown();
    initStatusFilter();
    initScanModal();
    showSkeletonGrid();
    send('getSettings');
    send('getLibrary');
    send('getRunningGames');
    send('getGroups');
    // Fallback poll at 10s (primary detection is via Process.Exited event)
    setInterval(() => send('getRunningGames'), 10000);
});

function initSortAndGrid() {
    const sortSelect = document.getElementById('sortSelect');
    sortSelect.addEventListener('change', () => {
        state.sortBy = sortSelect.value;
        state.settings.sortBy = sortSelect.value;
        saveSettingsFromUI();
        renderGrid();
    });

    const sortDirBtn = document.getElementById('btnSortDir');
    if (sortDirBtn) {
        sortDirBtn.addEventListener('click', () => {
            state.sortDir = state.sortDir === 'asc' ? 'desc' : 'asc';
            state.settings.sortDir = state.sortDir;
            sortDirBtn.classList.toggle('sort-desc', state.sortDir === 'desc');
            saveSettingsFromUI();
            renderGrid();
        });
    }

    document.querySelectorAll('.grid-toggle-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.grid-toggle-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            state.gridSize = btn.dataset.size;
            state.settings.gridSize = btn.dataset.size;
            saveSettingsFromUI();
            renderGrid();
        });
    });
}

function restoreSortAndGridUI() {
    const sortSelect = document.getElementById('sortSelect');
    if (sortSelect) sortSelect.value = state.sortBy;
    const sortDirBtn = document.getElementById('btnSortDir');
    if (sortDirBtn) sortDirBtn.classList.toggle('sort-desc', state.sortDir === 'desc');
    document.querySelectorAll('.grid-toggle-btn').forEach(b => {
        b.classList.toggle('active', b.dataset.size === state.gridSize);
    });
}

function initTagDropdown() {
    const btn = document.getElementById('btnTagDropdown');
    if (btn) btn.addEventListener('click', () => toggleTagDropdown());
}

function initStatusFilter() {
    const sel = document.getElementById('statusFilter');
    if (sel) sel.addEventListener('change', () => {
        state.filterStatus = parseInt(sel.value, 10);
        renderGrid();
    });
}

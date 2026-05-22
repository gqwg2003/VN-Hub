function initNavigation() {
    document.querySelectorAll('.nav-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            const tab = btn.dataset.tab;
            switchTab(tab);
        });
    });
}

function initSidebarCollapse() {
    const sidebar = document.getElementById('sidebar');
    const btn = document.getElementById('sidebarCollapseBtn');
    const root = document.documentElement;

    function setCollapsed(collapsed) {
        if (collapsed) {
            root.dataset.sidebarConfiguredWidth = getComputedStyle(root).getPropertyValue('--sidebar-width').trim();
            root.style.setProperty('--sidebar-width', '56px');
            sidebar.classList.add('collapsed');
        } else {
            root.style.setProperty('--sidebar-width', root.dataset.sidebarConfiguredWidth || '220px');
            sidebar.classList.remove('collapsed');
        }
        localStorage.setItem('sidebar-collapsed', collapsed ? '1' : '0');
    }

    btn.addEventListener('click', () => setCollapsed(!sidebar.classList.contains('collapsed')));

    if (localStorage.getItem('sidebar-collapsed') === '1') {
        sidebar.classList.add('collapsed');
        root.style.setProperty('--sidebar-width', '56px');
    }
}

function switchTab(tab) {
    state.activeTab = tab;
    document.querySelectorAll('.nav-btn').forEach(b => b.classList.toggle('active', b.dataset.tab === tab));

    const searchInput = document.getElementById('searchInput');
    if (searchInput && state.searchQuery) {
        searchInput.value = '';
        state.searchQuery = '';
        clearTimeout(state.searchTimer);
    }

    const isSettings = tab === 'settings';
    const isStats = tab === 'statistics';
    const isMarked = tab === 'marked';
    document.getElementById('contentLibrary').style.display = (isSettings || isStats) ? 'none' : '';
    document.getElementById('contentSettings').style.display = isSettings ? '' : 'none';
    document.getElementById('contentStatistics').style.display = isStats ? '' : 'none';
    document.getElementById('markedTabs').style.display = isMarked ? '' : 'none';

    const topbarControls = document.querySelector('.topbar-controls');
    if (topbarControls) topbarControls.style.display = (isSettings || isStats) ? 'none' : '';

    if (isStats) {
        send('getStats');
        send('getPlayStats', { days: state._playStatsDays || 30 });
    } else if (!isSettings) {
        loadTabData();
    }
}

function initMarkedTabs() {
    document.querySelectorAll('.marked-tab').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.marked-tab').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            state.markedSubTab = btn.dataset.subtab;
            refreshLibrary();
        });
    });
}

function loadTabData() {
    refreshLibrary();
}

function refreshLibrary() {
    send('getLibrary', buildLibraryQuery());
}

function buildLibraryQuery() {
    return {
        tab: state.activeTab,
        markedSubTab: state.markedSubTab,
        status: typeof state.filterStatus === 'number' ? state.filterStatus : -1,
        groupId: state.activeGroupId || null,
        tag: state.activeTag || null,
        search: state.searchQuery || null,
        sortBy: state.sortBy || 'title',
        sortDir: state.sortDir || 'asc'
    };
}

function initSearch() {
    const input = document.getElementById('searchInput');
    input.addEventListener('input', () => {
        clearTimeout(state.searchTimer);
        state.searchTimer = setTimeout(() => {
            state.searchQuery = input.value.trim();
            refreshLibrary();
        }, 300);
    });

    document.addEventListener('keydown', (e) => {
        if (typeof shortcutMatches === 'function' && shortcutMatches('search', e)) {
            e.preventDefault();
            input.focus();
            return;
        }
        if (typeof shortcutMatches === 'function' && shortcutMatches('addVn', e)) {
            e.preventDefault();
            document.getElementById('addModal').style.display = '';
            const titleInput = document.getElementById('addTitle');
            if (titleInput) titleInput.focus();
            return;
        }
        if (typeof shortcutMatches === 'function' && shortcutMatches('delete', e) && state.detailEntry) {
            const active = document.activeElement;
            if (!active || (active.tagName !== 'INPUT' && active.tagName !== 'TEXTAREA' && active.tagName !== 'SELECT')) {
                document.getElementById('deleteModal').style.display = '';
            }
            return;
        }
        if (e.key === 'F11') {
            e.preventDefault();
            toggleFullscreen();
            return;
        }
        if (e.key === 'Escape') {
            closeDetail();
            closeAddModal();
            closeDeleteModal();
        }
    });
}

function toggleFullscreen() {
    send('toggleFullscreen');
}

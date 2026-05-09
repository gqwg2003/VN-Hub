/* ===== Settings ===== */

function initSettings() {
    document.querySelectorAll('.settings-tab').forEach(tab => {
        tab.addEventListener('click', () => {
            document.querySelectorAll('.settings-tab').forEach(t => t.classList.remove('active'));
            document.querySelectorAll('.settings-tab-content').forEach(c => c.classList.remove('active'));
            tab.classList.add('active');
            const content = document.querySelector(`.settings-tab-content[data-tab-content="${tab.dataset.tab}"]`);
            if (content) content.classList.add('active');
        });
    });

    document.querySelectorAll('.theme-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.theme-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            state.settings.theme = btn.dataset.theme;
            applyTheme(btn.dataset.theme);
            saveSettingsFromUI();
        });
    });

    document.getElementById('btnPickDefaultFolder').addEventListener('click', () => {
        state.pickTarget = 'settings';
        send('pickFolder');
    });

    document.getElementById('btnOpenDbFolder').addEventListener('click', () => {
        send('openDbFolder');
    });

    document.getElementById('settingsProxy').addEventListener('change', (e) => {
        const val = e.target.value.trim();
        if (val && !/^https?:\/\/.+:\d+/.test(val)) {
            showToast(t('invalidProxy') || 'Invalid proxy format. Use http://host:port', 'error');
            return;
        }
        state.settings.proxyAddress = val;
        saveSettingsFromUI();
    });

    document.getElementById('settingsLanguage').addEventListener('change', (e) => {
        state.settings.language = e.target.value;
        setLanguage(e.target.value);
        saveSettingsFromUI();
    });

    document.getElementById('btnExportLibrary').addEventListener('click', () => {
        send('exportLibrary');
    });

    document.getElementById('btnImportLibrary').addEventListener('click', () => {
        send('importLibrary');
    });

    document.getElementById('btnExportCsv').addEventListener('click', () => {
        send('exportCsv');
    });

    document.getElementById('btnExportHtml').addEventListener('click', () => {
        send('exportHtml');
    });

    document.getElementById('settingsVndb').addEventListener('change', (e) => {
        state.settings.vndbEnabled = e.target.checked;
        saveSettingsFromUI();
    });

    document.getElementById('settingsAutoStart').addEventListener('change', (e) => {
        state.settings.autoStart = e.target.checked;
        send('setAutoStart', { enabled: e.target.checked });
        saveSettingsFromUI();
    });

    document.getElementById('settingsMinimizeToTray').addEventListener('change', (e) => {
        state.settings.minimizeToTray = e.target.checked;
        saveSettingsFromUI();
    });

    document.getElementById('settingsStartMinimized').addEventListener('change', (e) => {
        state.settings.startMinimized = e.target.checked;
        saveSettingsFromUI();
    });

    document.getElementById('btnBackupNow').addEventListener('click', () => {
        send('backupNow');
    });

    document.getElementById('btnShowBackups').addEventListener('click', () => {
        const list = document.getElementById('backupList');
        if (list.style.display === 'none') {
            send('getBackups');
            list.style.display = '';
        } else {
            list.style.display = 'none';
        }
    });

    document.getElementById('settingsBackupInterval').addEventListener('change', (e) => {
        state.settings.backupInterval = e.target.value;
        saveSettingsFromUI();
    });

    document.getElementById('settingsMaxBackups').addEventListener('change', (e) => {
        state.settings.maxBackups = parseInt(e.target.value) || 5;
        saveSettingsFromUI();
    });

    document.getElementById('btnOpenCoversFolder').addEventListener('click', () => {
        send('openCoversFolder');
    });

    document.getElementById('btnOpenLogsFolder').addEventListener('click', () => {
        send('openLogsFolder');
    });

    document.getElementById('btnClearLogs').addEventListener('click', () => {
        send('clearLogs');
    });

    document.getElementById('settingsScanBlacklistExe').addEventListener('change', (e) => {
        state.settings.scanBlacklistExe = e.target.value.split('\n').map(s => s.trim()).filter(Boolean);
        saveSettingsFromUI();
    });
    document.getElementById('settingsScanBlacklistDirs').addEventListener('change', (e) => {
        state.settings.scanBlacklistDirs = e.target.value.split('\n').map(s => s.trim()).filter(Boolean);
        saveSettingsFromUI();
    });
    document.getElementById('settingsScanSortBy').addEventListener('change', (e) => {
        state.settings.scanSortBy = e.target.value;
        saveSettingsFromUI();
    });
    document.getElementById('settingsScanSortDir').addEventListener('change', (e) => {
        state.settings.scanSortDir = e.target.value;
        saveSettingsFromUI();
    });
    document.getElementById('settingsScanSkipExisting').addEventListener('change', (e) => {
        state.settings.scanSkipExisting = e.target.checked;
        saveSettingsFromUI();
    });
    document.getElementById('settingsScanRecursive').addEventListener('change', (e) => {
        state.settings.scanRecursive = e.target.checked;
        saveSettingsFromUI();
    });

    document.getElementById('btnResetSettings').addEventListener('click', () => {
        if (!confirm(t('resetSettingsConfirm'))) return;
        const preserved = {
            dbPath: state.settings.dbPath,
            coversPath: state.settings.coversPath,
            logsPath: state.settings.logsPath,
            windowX: state.settings.windowX,
            windowY: state.settings.windowY,
            windowWidth: state.settings.windowWidth,
            windowHeight: state.settings.windowHeight,
            windowMaximized: state.settings.windowMaximized
        };
        state.settings = {
            theme: 'dark',
            defaultFolder: '',
            language: 'en',
            vndbEnabled: true,
            autoStart: false,
            minimizeToTray: true,
            startMinimized: true,
            maxBackups: 5,
            backupInterval: 'startup',
            shortcuts: {},
            proxyAddress: '',
            sortBy: 'title',
            sortDir: 'asc',
            gridSize: 'medium',
            scanBlacklistExe: [],
            scanBlacklistDirs: [],
            scanSortBy: 'title',
            scanSortDir: 'asc',
            scanSkipExisting: true,
            scanRecursive: false,
            ...preserved
        };
        applyTheme('dark');
        setLanguage('en');
        state.sortBy = 'title';
        state.sortDir = 'asc';
        state.gridSize = 'medium';
        restoreSortAndGridUI();
        renderSettings();
        saveSettingsFromUI();
        send('setAutoStart', { enabled: false });
        showToast(t('resetSettingsDone'), 'success');
    });

    initShortcutButtons();
}

function renderSettings() {
    const s = state.settings;
    document.getElementById('settingsDefaultFolder').value = s.defaultFolder || '';
    document.getElementById('settingsDbPath').value = s.dbPath || '';
    document.getElementById('settingsCoversPath').value = s.coversPath || '';
    document.getElementById('settingsLanguage').value = s.language || 'en';
    document.querySelectorAll('.theme-btn').forEach(b => {
        b.classList.toggle('active', b.dataset.theme === s.theme);
    });
    document.getElementById('settingsVndb').checked = s.vndbEnabled !== false;
    document.getElementById('settingsAutoStart').checked = s.autoStart === true;
    document.getElementById('settingsMinimizeToTray').checked = s.minimizeToTray !== false;
    document.getElementById('settingsStartMinimized').checked = s.startMinimized === true;
    document.getElementById('settingsBackupInterval').value = s.backupInterval || 'startup';
    document.getElementById('settingsMaxBackups').value = s.maxBackups || 5;
    document.getElementById('settingsProxy').value = s.proxyAddress || '';

    document.getElementById('settingsScanBlacklistExe').value = (s.scanBlacklistExe || []).join('\n');
    document.getElementById('settingsScanBlacklistDirs').value = (s.scanBlacklistDirs || []).join('\n');
    document.getElementById('settingsScanSortBy').value = s.scanSortBy || 'title';
    document.getElementById('settingsScanSortDir').value = s.scanSortDir || 'asc';
    document.getElementById('settingsScanSkipExisting').checked = s.scanSkipExisting !== false;
    document.getElementById('settingsScanRecursive').checked = s.scanRecursive === true;
    renderShortcutButtons();
}

function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme || 'dark');
}

function saveSettingsFromUI() {
    state.settings.defaultFolder = document.getElementById('settingsDefaultFolder').value;
    send('saveSettings', state.settings);
}

/* ===== Configurable Shortcuts ===== */

const DEFAULT_SHORTCUTS = {
    addVn: 'Ctrl+N',
    search: 'Ctrl+F',
    delete: 'Delete'
};

function getShortcuts() {
    const custom = state.settings.shortcuts || {};
    return { ...DEFAULT_SHORTCUTS, ...custom };
}

function initShortcutButtons() {
    document.querySelectorAll('.shortcut-key-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.shortcut-key-btn.recording').forEach(b => {
                b.classList.remove('recording');
            });
            btn.classList.add('recording');
            btn.innerHTML = '<kbd>...</kbd>';

            const downHandler = (e) => {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();

                if (e.key === 'Escape' && !e.ctrlKey && !e.shiftKey && !e.altKey) {
                    cleanup();
                    renderShortcutButtons();
                    return;
                }

                if (['Control', 'Shift', 'Alt', 'Meta'].includes(e.key)) return;

                const parts = [];
                if (e.ctrlKey) parts.push('Ctrl');
                if (e.shiftKey) parts.push('Shift');
                if (e.altKey) parts.push('Alt');
                const key = e.key.length === 1 ? e.key.toUpperCase() : e.key;
                parts.push(key);

                const combo = parts.join('+');
                const action = btn.dataset.action;
                if (!state.settings.shortcuts) state.settings.shortcuts = {};
                state.settings.shortcuts[action] = combo;
                saveSettingsFromUI();
                cleanup();
                renderShortcutButtons();
            };

            const cleanup = () => {
                btn.classList.remove('recording');
                document.removeEventListener('keydown', downHandler, true);
            };

            document.addEventListener('keydown', downHandler, true);
        });
    });
}

function renderShortcutButtons() {
    const sc = getShortcuts();
    document.querySelectorAll('.shortcut-key-btn').forEach(btn => {
        const action = btn.dataset.action;
        const combo = sc[action] || '';
        btn.innerHTML = combo.split('+').map(k => `<kbd>${k}</kbd>`).join('+');
    });
}

function shortcutMatches(action, e) {
    const sc = getShortcuts();
    const combo = sc[action];
    if (!combo) return false;
    const parts = combo.split('+');
    const needCtrl = parts.includes('Ctrl');
    const needShift = parts.includes('Shift');
    const needAlt = parts.includes('Alt');
    const key = parts.filter(p => !['Ctrl', 'Shift', 'Alt'].includes(p))[0];
    if (!key) return false;
    if (needCtrl !== e.ctrlKey) return false;
    if (needShift !== e.shiftKey) return false;
    if (needAlt !== e.altKey) return false;
    const eKey = e.key.length === 1 ? e.key.toUpperCase() : e.key;
    return eKey === key;
}

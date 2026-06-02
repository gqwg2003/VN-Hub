function send(action, payload) {
    window.chrome.webview.postMessage(JSON.stringify({ action, payload }));
}

const bridgeHandlers = {};

window.chrome.webview.addEventListener('message', (event) => {
    try {
        const msg = event.data;
        const handler = bridgeHandlers[msg.callback];
        if (handler) {
            handler(msg.data);
        } else {
            console.warn('Unknown bridge callback:', msg.callback);
        }
    } catch (err) {
        console.error('Bridge message error:', err);
    }
});

Object.assign(bridgeHandlers, {
    receiveLibrary(entries) {
        state.entries = entries || [];
        state._tagsCache = null;
        renderGrid();
        renderGroupsSidebar();
        updateSearchResultCount();
        dismissSplash();
    },
    receiveTags(tags) {
        state._tagsCache = Array.isArray(tags) ? tags : [];
        const dd = document.getElementById('tagDropdown');
        if (dd && dd.classList.contains('open') && typeof renderTagDropdown === 'function') {
            renderTagDropdown(dd);
        }
    },
    vnAdded(entry) {
        if (!entry) return;
        state.entries.unshift(entry);
        state._tagsCache = null;
        renderGrid();
        closeAddModal();
        state._statsCache = null;
    },
    vnUpdated(entry) {
        if (!entry) return;
        const idx = state.entries.findIndex(e => e.id === entry.id);
        if (idx >= 0) state.entries[idx] = entry;
        else state.entries.unshift(entry);
        state._tagsCache = null;
        patchCard(entry);
        if (state.detailEntry?.id === entry.id) {
            state.detailEntry = entry;
            renderDetail();
        }
        state._statsCache = null;
    },
    vnDeleted(data) {
        if (!data) return;
        const entry = state.entries.find(e => e.id === data.id);
        state.entries = state.entries.filter(e => e.id !== data.id);
        state._tagsCache = null;
        renderGrid();
        closeDetail();
        state._statsCache = null;
        if (entry) {
            state._deletedStack.push(entry);
            showUndoToast(t('vnDeletedUndo') || '"{0}" deleted.'.replace('{0}', entry.title), () => {
                const restored = state._deletedStack.pop();
                if (restored) send('addVn', { title: restored.title, exePath: restored.exePath });
            });
        }
    },
    folderPicked(data) {
        if (!data) return;
        if (state.pickTarget === 'settings') {
            document.getElementById('settingsDefaultFolder').value = data.path;
            saveSettingsFromUI();
        }
        state.pickTarget = null;
    },
    imagePicked(data) {
        if (!data) return;
        if (state.detailEntry) {
            send('setCover', { id: state.detailEntry.id, sourcePath: data.path });
        }
    },
    exePicked(data) {
        if (!data) return;
        if (state.pickTarget === 'addExe') {
            document.getElementById('addExe').value = data.path;
            const titleInput = document.getElementById('addTitle');
            if (!titleInput.value.trim() && data.suggestedTitle) {
                titleInput.value = data.suggestedTitle;
            }
            state.pickTarget = null;
        } else if (state.detailEntry) {
            state.detailEntry.exePath = data.path;
            document.getElementById('detailExe').value = data.path;
            saveDetail();
            if (!state.detailEntry.coverPath) {
                send('extractIcon', { id: state.detailEntry.id, exePath: data.path });
            }
        }
    },
    launchResult(data) {
        if (data && !data.success) {
            showToast(t('launchError'), 'error');
        }
    },
    gameStarted(data) {
        if (data?.id) {
            state.runningGames.add(data.id);
            patchRunningCards([data.id]);
            updateLaunchButton();
        }
    },
    gameStopped(data) {
        if (data?.id) {
            state.runningGames.delete(data.id);
            state._statsCache = null;
            refreshLibrary();
            updateLaunchButton();
        }
    },
    runningGames(data) {
        if (data?.ids) {
            const newSet = new Set(data.ids);
            const added = data.ids.filter(id => !state.runningGames.has(id));
            const removed = [...state.runningGames].filter(id => !newSet.has(id));
            state.runningGames = newSet;
            if (added.length || removed.length) {
                patchRunningCards([...added, ...removed]);
                updateLaunchButton();
            }
        }
    },
    receiveSettings(settings) {
        if (!settings) return;
        state.settings = settings;
        applyTheme(settings.theme);
        if (settings.language) setLanguage(settings.language);
        if (settings.sortBy) state.sortBy = settings.sortBy;
        if (settings.sortDir) state.sortDir = settings.sortDir;
        if (settings.gridSize) state.gridSize = settings.gridSize;
        restoreSortAndGridUI();
        renderSettings();
        if (typeof applyCustomization === 'function') applyCustomization();
        if (typeof renderCustomization === 'function') renderCustomization();
    },
    fontAdded(data) {
        if (!data) return;
        const c = ensureCustomization();
        c.fonts = data.fonts || c.fonts;
        c.activeFont = data.activeFont || data.fileName || c.activeFont;
        applyCustomization();
        renderCustomization();
        showToast(t('fontAddedToast').replace('{0}', data.fileName || ''), 'success');
    },
    fontRemoved(data) {
        if (!data) return;
        const c = ensureCustomization();
        c.fonts = data.fonts || [];
        c.activeFont = data.activeFont || '';
        applyCustomization();
        renderCustomization();
        showToast(t('fontRemovedToast'), 'success');
    },
    fontsList(data) {
        const c = ensureCustomization();
        c.fonts = data?.fonts || [];
        renderCustomFontList();
    },
    backgroundPicked(data) {
        const c = ensureCustomization();
        c.backgroundImage = data?.fileName || '';
        applyCustomization();
        renderCustomBackground();
        showToast(t('backgroundSetToast'), 'success');
    },
    backgroundCleared() {
        const c = ensureCustomization();
        c.backgroundImage = '';
        applyCustomization();
        renderCustomBackground();
        showToast(t('backgroundClearedToast'), 'success');
    },
    sidebarBackgroundPicked(data) {
        const c = ensureCustomization();
        c.sidebarBackgroundImage = data?.fileName || '';
        applyCustomization();
        renderSidebarBackground();
        showToast(t('backgroundSetToast'), 'success');
    },
    sidebarBackgroundCleared() {
        const c = ensureCustomization();
        c.sidebarBackgroundImage = '';
        applyCustomization();
        renderSidebarBackground();
        showToast(t('backgroundClearedToast'), 'success');
    },
    topbarBackgroundPicked(data) {
        const c = ensureCustomization();
        c.topbarBackgroundImage = data?.fileName || '';
        applyCustomization();
        renderTopbarBackground();
        showToast(t('backgroundSetToast'), 'success');
    },
    topbarBackgroundCleared() {
        const c = ensureCustomization();
        c.topbarBackgroundImage = '';
        applyCustomization();
        renderTopbarBackground();
        showToast(t('backgroundClearedToast'), 'success');
    },
    settingsSaved(data) {
        if (data && typeof data.proxyAddress === 'string' && state.settings) {
            state.settings.proxyAddress = data.proxyAddress;
            const input = document.getElementById('settingsProxy');
            if (input && input.value !== data.proxyAddress) input.value = data.proxyAddress;
        }
    },
    exportDone(data) {
        if (data?.path) showToast(t('exportDone') + ' ' + data.path, 'success');
    },
    importDone(data) {
        if (data?.count >= 0) {
            showToast(t('importDone').replace('{0}', data.count), 'success');
            refreshLibrary();
            state._statsCache = null;
        }
    },
    onError(data) {
        console.error('Bridge error:', data?.message);
    },
    receiveStats(data) {
        if (data && typeof renderStats === 'function') {
            state._statsCache = data;
            renderStats(data);
        }
    },
    receiveGroups(groups) {
        state.groups = groups || [];
        renderGroupsSidebar();
    },
    vndbResult(data) {
        if (!data) return;
        if (data.disabled) {
            showToast(t('vndbDisabled'), 'warning');
        } else if (data.found) {
            let msg = t('vndbFound').replace('{0}', data.title || '');
            if (data.coverError) msg += ' (cover: ' + data.coverError + ')';
            showToast(msg, data.coverError ? 'warning' : 'success');
        } else {
            const err = data.error ? ': ' + data.error : '';
            showToast(t('vndbNotFound') + err, 'warning');
        }
    },
    autoStartSet() { },
    receiveBackups(data) {
        const list = document.getElementById('backupList');
        if (!list) return;
        if (!data || data.length === 0) {
            list.innerHTML = `<p class="settings-hint">${t('noBackups')}</p>`;
            return;
        }
        list.innerHTML = data.map(b => {
            const date = new Date(b.date).toLocaleString();
            return `<div class="backup-item">
                <span class="backup-info"><strong>${escapeHTML(b.fileName)}</strong><br>${date} — ${b.sizeKb} KB</span>
                <button class="btn-secondary btn-small backup-restore-btn" data-file="${escapeAttr(b.fileName)}">${t('restoreBackup')}</button>
            </div>`;
        }).join('');
        list.querySelectorAll('.backup-restore-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                if (confirm(t('restoreConfirm'))) {
                    send('restoreBackup', { fileName: btn.dataset.file });
                }
            });
        });
    },
    backupRestored() {
        showToast(t('backupRestoredOk'), 'success');
        refreshLibrary();
        state._statsCache = null;
    },
    backupDone(data) {
        showToast(t('backupCreated') || 'Backup created successfully', 'success');
    },
    receiveSessions(data) {
        if (data && typeof renderSessionHistory === 'function') {
            renderSessionHistory(data.sessions || []);
        }
    },
    receivePlayStats(data) {
        if (data && typeof renderPlayStats === 'function') {
            renderPlayStats(data);
        }
    },
    logsCleaned(data) {
        if (data) showToast(t('logsClearedMsg').replace('{0}', data.count || 0), 'success');
    },
    scanResults(data) {
        if (!data) return;
        renderScanResults(data.items || []);
    },
    bulkAddDone(data) {
        document.getElementById('scanModal').style.display = 'none';
        showToast((t('bulkAddDone') || '{0} VNs added.').replace('{0}', data?.count || 0), 'success');
    }
});

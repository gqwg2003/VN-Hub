/* ===== Add Modal ===== */

function initAddModal() {
    document.getElementById('btnAdd').addEventListener('click', openAddModal);
    document.getElementById('btnAddCancel').addEventListener('click', closeAddModal);
    document.getElementById('btnAddConfirm').addEventListener('click', confirmAdd);

    document.getElementById('btnAddPickExe').addEventListener('click', () => {
        state.pickTarget = 'addExe';
        send('pickExe');
    });

    document.getElementById('addTitle').addEventListener('keydown', (e) => {
        if (e.key === 'Enter') confirmAdd();
    });
}

function openAddModal() {
    document.getElementById('addTitle').value = '';
    document.getElementById('addExe').value = '';
    document.getElementById('addSkipVndb').checked = false;
    const skipWrap = document.getElementById('addSkipVndbWrap');
    if (skipWrap) skipWrap.style.display = state.settings?.vndbEnabled !== false ? '' : 'none';
    document.getElementById('addModal').style.display = '';
    document.getElementById('addTitle').focus();
}

function closeAddModal() {
    document.getElementById('addModal').style.display = 'none';
}

function confirmAdd() {
    const title = document.getElementById('addTitle').value.trim();
    const exePath = document.getElementById('addExe').value || null;
    const skipVndb = document.getElementById('addSkipVndb').checked;

    if (!title && !exePath) {
        document.getElementById('addTitle').focus();
        return;
    }
    send('addVn', { title, path: null, exePath, skipVndb });
}

/* ===== Scan Modal ===== */

function initScanModal() {
    document.getElementById('btnScanFolder').addEventListener('click', () => {
        document.getElementById('scanResults').innerHTML = '';
        document.getElementById('btnScanAdd').style.display = 'none';
        document.getElementById('scanStatus').style.display = '';
        document.getElementById('scanModal').style.display = '';
        send('scanFolder');
    });

    document.getElementById('btnScanCancel').addEventListener('click', () => {
        document.getElementById('scanModal').style.display = 'none';
    });

    document.getElementById('btnScanAdd').addEventListener('click', () => {
        const items = [];
        document.querySelectorAll('.scan-item input[type="checkbox"]:checked').forEach(cb => {
            const row = cb.closest('.scan-item');
            items.push({
                title: row.dataset.title,
                exePath: row.querySelector('.scan-exe-select')?.value || row.dataset.exe
            });
        });
        if (items.length === 0) return;
        document.getElementById('scanStatus').style.display = '';
        document.getElementById('btnScanAdd').style.display = 'none';
        send('bulkAddScanned', { items });
    });
}

function renderScanResults(results) {
    document.getElementById('scanStatus').style.display = 'none';
    const container = document.getElementById('scanResults');
    const addBtn = document.getElementById('btnScanAdd');

    if (!results || results.length === 0) {
        container.innerHTML = `<p class="settings-hint">${t('scanEmpty') || 'No VNs found in selected folder.'}</p>`;
        addBtn.style.display = 'none';
        return;
    }

    const skipExisting = state.settings?.scanSkipExisting !== false;
    const newResults = skipExisting ? results.filter(r => !r.alreadyExists) : results;
    const existingCount = results.length - newResults.length;

    let html = '';
    if (existingCount > 0) {
        html += `<p class="settings-hint">${(t('scanSkipped') || '{0} already in library').replace('{0}', existingCount)}</p>`;
    }

    for (const r of newResults) {
        const exeOptions = (r.exes || []).map(exe =>
            `<option value="${escapeAttr(exe)}"${exe === r.selectedExe ? ' selected' : ''}>${escapeHTML(exe.split('\\').pop())}</option>`
        ).join('');

        html += `
        <div class="scan-item" data-title="${escapeAttr(r.title)}" data-exe="${escapeAttr(r.selectedExe || '')}">
            <label class="scan-item-check">
                <input type="checkbox" checked>
                <span class="scan-item-title">${escapeHTML(r.title)}</span>
            </label>
            ${r.exes && r.exes.length > 1
                ? `<select class="scan-exe-select detail-select">${exeOptions}</select>`
                : `<span class="scan-exe-path">${escapeHTML((r.selectedExe || '').split('\\').pop())}</span>`
            }
        </div>`;
    }

    container.innerHTML = html;
    addBtn.style.display = newResults.length > 0 ? '' : 'none';
}

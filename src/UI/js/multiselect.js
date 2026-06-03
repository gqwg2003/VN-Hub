function initMultiSelect() {
    state.selectedIds = new Set();

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && state.selectedIds.size > 0) {
            clearSelection();
        }
    });
}

function toggleSelect(id) {
    if (state.selectedIds.has(id)) {
        state.selectedIds.delete(id);
    } else {
        state.selectedIds.add(id);
    }
    updateSelectionUI();
}

function clearSelection() {
    state.selectedIds.clear();
    updateSelectionUI();
}

function updateSelectionUI() {
    document.querySelectorAll('.vn-card').forEach(card => {
        card.classList.toggle('selected', state.selectedIds.has(card.dataset.id));
    });
    updateBulkBar();
}

function updateBulkBar() {
    let bar = document.getElementById('bulkBar');
    const count = state.selectedIds.size;

    if (count === 0) {
        if (bar) bar.classList.remove('visible');
        return;
    }

    if (!bar) {
        bar = document.createElement('div');
        bar.id = 'bulkBar';
        bar.className = 'bulk-bar';
        document.body.appendChild(bar);
    }

    const statusLabels = getStatusLabels();
    const statusOptions = statusLabels.map((lbl, i) =>
        `<option value="${i}">${escapeHTML(lbl)}</option>`
    ).join('');

    const groupOptions = (state.groups || []).map(g =>
        `<option value="${escapeAttr(g.id)}">${escapeHTML(g.name)}</option>`
    ).join('');

    bar.innerHTML = `
        <span class="bulk-bar-count">${count} ${t('selected') || 'selected'}</span>
        <button class="bulk-bar-btn" id="bulkFav">${t('favorite')}</button>
        <button class="bulk-bar-btn" id="bulkPin">${t('pin')}</button>
        <select class="bulk-bar-btn" id="bulkStatus">
            <option value="" disabled selected>${t('status')}</option>
            ${statusOptions}
        </select>
        <select class="bulk-bar-btn" id="bulkGroup">
            <option value="" disabled selected>${t('group') || 'Group'}</option>
            <option value="__none__">${t('noGroup') || 'No Group'}</option>
            ${groupOptions}
        </select>
        <button class="bulk-bar-btn" id="bulkAddTag">${t('addTag') || '+ Tag'}</button>
        <button class="bulk-bar-btn" id="bulkRemoveTag">${t('removeTag') || '- Tag'}</button>
        <button class="bulk-bar-btn danger" id="bulkDelete">${t('delete')}</button>
        <button class="bulk-bar-close" id="bulkClose">&times;</button>
    `;

    bar.querySelector('#bulkFav').addEventListener('click', bulkToggleFavorite);
    bar.querySelector('#bulkPin').addEventListener('click', bulkTogglePin);
    bar.querySelector('#bulkStatus').addEventListener('change', bulkSetStatus);
    bar.querySelector('#bulkGroup').addEventListener('change', bulkSetGroup);
    bar.querySelector('#bulkAddTag').addEventListener('click', bulkAddTag);
    bar.querySelector('#bulkRemoveTag').addEventListener('click', bulkRemoveTag);
    bar.querySelector('#bulkDelete').addEventListener('click', bulkDelete);
    bar.querySelector('#bulkClose').addEventListener('click', clearSelection);

    bar.classList.add('visible');
}

function bulkToggleFavorite() {
    const ids = [...state.selectedIds];
    if (ids.length === 0) return;
    send('bulkSetFavorite', { ids, value: true });
    clearSelection();
}

function bulkTogglePin() {
    const ids = [...state.selectedIds];
    if (ids.length === 0) return;
    send('bulkSetPin', { ids, value: true });
    clearSelection();
}

function bulkSetStatus() {
    const select = document.getElementById('bulkStatus');
    const status = parseInt(select.value, 10);
    if (isNaN(status)) return;
    const ids = [...state.selectedIds];
    if (ids.length === 0) return;
    send('bulkSetStatus', { ids, status });
    clearSelection();
}
function bulkSetGroup() {
    const select = document.getElementById('bulkGroup');
    const val = select.value;
    if (!val) return;
    const groupId = val === '__none__' ? null : val;
    const ids = [...state.selectedIds];
    if (ids.length === 0) return;
    send('bulkSetGroup', { ids, groupId });
    clearSelection();
}

async function bulkAddTag() {
    const ids = [...state.selectedIds];
    if (ids.length === 0) return;
    const tag = await showPromptModal(t('bulkTagPrompt') || 'Enter tag to add:');
    if (!tag || !tag.trim()) return;
    send('bulkAddTag', { ids, tag: tag.trim() });
    clearSelection();
}

async function bulkRemoveTag() {
    const ids = [...state.selectedIds];
    if (ids.length === 0) return;
    const tag = await showPromptModal(t('bulkRemoveTagPrompt') || 'Enter tag to remove:');
    if (!tag || !tag.trim()) return;
    send('bulkRemoveTag', { ids, tag: tag.trim() });
    clearSelection();
}

function bulkDelete() {
    const count = state.selectedIds.size;
    if (count === 0) return;
    const text = (t('bulkDeleteMessage') || 'This will permanently remove {0} VNs from your library.').replace('{0}', count);
    document.getElementById('bulkDeleteText').textContent = text;
    document.getElementById('bulkDeleteModal').style.display = '';

    const confirmBtn = document.getElementById('btnBulkDeleteConfirm');
    const cancelBtn = document.getElementById('btnBulkDeleteCancel');
    const overlay = document.getElementById('bulkDeleteModal');

    const cleanup = () => {
        overlay.style.display = 'none';
        confirmBtn.removeEventListener('click', onConfirm);
        cancelBtn.removeEventListener('click', onCancel);
    };
    const onConfirm = () => {
        const ids = [...state.selectedIds];
        if (ids.length > 0) send('bulkDelete', { ids });
        clearSelection();
        cleanup();
    };
    const onCancel = () => {
        cleanup();
    };
    confirmBtn.addEventListener('click', onConfirm);
    cancelBtn.addEventListener('click', onCancel);
}

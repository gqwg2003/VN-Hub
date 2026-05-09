/* ===== Groups / Categories ===== */

function initGroups() {
    state.groups = [];
    state.activeGroupId = null;
}

function getGroupVnCount(groupId) {
    if (!state.entries) return 0;
    return state.entries.filter(e => e.groupId === groupId).length;
}

function renderGroupsSidebar() {
    let container = document.getElementById('groupList');
    if (!container) return;

    const totalCount = state.entries ? state.entries.length : 0;
    let html = `<div class="group-item${state.activeGroupId === null ? ' active' : ''}" data-group="">
        <span class="group-dot" style="background:#888"></span>
        <span>${t('allGroups')}</span>
        <span class="group-count">${totalCount}</span>
    </div>`;

    state.groups.forEach(g => {
        const count = getGroupVnCount(g.id);
        html += `<div class="group-item${state.activeGroupId === g.id ? ' active' : ''}" data-group="${escapeAttr(g.id)}">
            <span class="group-dot" style="background:${escapeAttr(g.color)}"></span>
            <span class="group-name">${escapeHTML(g.name)}</span>
            <span class="group-count">${count}</span>
            <button class="group-edit-btn" data-gid="${escapeAttr(g.id)}" title="${t('renameGroup')}">✎</button>
            <button class="group-color-btn" data-gid="${escapeAttr(g.id)}" title="${t('groupColor')}">
                <span class="group-color-swatch" style="background:${escapeAttr(g.color)}"></span>
            </button>
            <button class="group-delete-btn" data-gid="${escapeAttr(g.id)}" title="${t('deleteGroup')}">&times;</button>
        </div>`;
    });

    html += `<div class="group-add" id="groupAddBtn">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
        <span>${t('addGroup')}</span>
    </div>`;

    container.innerHTML = html;

    container.querySelectorAll('.group-item').forEach(el => {
        el.addEventListener('click', () => {
            const gid = el.dataset.group || null;
            state.activeGroupId = gid;
            renderGroupsSidebar();
            renderGrid();
        });
    });

    container.querySelectorAll('.group-delete-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.stopPropagation();
            const gid = btn.dataset.gid;
            const group = state.groups.find(g => g.id === gid);
            const name = group ? group.name : '';
            const ok = await showConfirmModal(
                t('deleteGroup'),
                (t('deleteGroupConfirm') || 'Delete group "{0}"?').replace('{0}', name),
                t('delete') || 'Delete'
            );
            if (ok) send('deleteGroup', { id: gid });
        });
    });

    container.querySelectorAll('.group-edit-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.stopPropagation();
            const gid = btn.dataset.gid;
            const group = state.groups.find(g => g.id === gid);
            if (!group) return;
            const newName = await showPromptModal(t('renameGroup'), group.name);
            if (newName && newName !== group.name) {
                send('updateGroup', { id: group.id, name: newName, color: group.color });
            }
        });
    });

    container.querySelectorAll('.group-color-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const gid = btn.dataset.gid;
            const group = state.groups.find(g => g.id === gid);
            if (!group) return;
            showGroupColorPicker(btn, group);
        });
    });

    document.getElementById('groupAddBtn')?.addEventListener('click', promptAddGroup);
}

async function promptAddGroup() {
    const name = await showPromptModal(t('groupName') || 'Group name');
    if (!name) return;

    const colors = ['#6366f1', '#f43f5e', '#10b981', '#f59e0b', '#3b82f6', '#8b5cf6', '#ec4899', '#14b8a6'];
    const color = colors[state.groups.length % colors.length];

    send('addGroup', { name, color });
}

function showGroupColorPicker(anchor, group) {
    document.querySelector('.group-color-picker')?.remove();

    const colors = ['#6366f1', '#f43f5e', '#10b981', '#f59e0b', '#3b82f6', '#8b5cf6', '#ec4899', '#14b8a6',
                    '#ef4444', '#22c55e', '#06b6d4', '#a855f7', '#f97316', '#64748b', '#e11d48', '#0ea5e9'];
    const picker = document.createElement('div');
    picker.className = 'group-color-picker';
    picker.innerHTML = colors.map(c =>
        `<span class="gcp-swatch${c === group.color ? ' active' : ''}" data-color="${c}" style="background:${c}"></span>`
    ).join('') + `<input type="color" class="gcp-custom" value="${group.color}" title="${t('groupColor')}">`;

    picker.querySelectorAll('.gcp-swatch').forEach(sw => {
        sw.addEventListener('click', (e) => {
            e.stopPropagation();
            send('updateGroup', { id: group.id, name: group.name, color: sw.dataset.color });
            picker.remove();
        });
    });
    picker.querySelector('.gcp-custom').addEventListener('input', (e) => {
        e.stopPropagation();
        send('updateGroup', { id: group.id, name: group.name, color: e.target.value });
        picker.remove();
    });

    anchor.parentElement.appendChild(picker);
    setTimeout(() => document.addEventListener('click', () => picker.remove(), { once: true }), 0);
}

function filterByGroup(entries) {
    if (!state.activeGroupId) return entries;
    return entries.filter(e => e.groupId === state.activeGroupId);
}

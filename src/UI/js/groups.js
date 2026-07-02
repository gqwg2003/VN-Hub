function initGroups() {
    state.groups = [];
    state.activeGroupId = null;
    installSmartGroupHook();
}

function parseGroupFilter(group) {
    if (!group || !group.filter) return null;
    try {
        const f = JSON.parse(group.filter);
        return (f && typeof f === 'object') ? f : null;
    } catch {
        return null;
    }
}

function vnMatchesFilter(entry, filter) {
    if (!entry || !filter) return false;
    if (typeof filter.status === 'number' && entry.status !== filter.status) return false;
    if (typeof filter.isFavorite === 'boolean' && !!entry.isFavorite !== filter.isFavorite) return false;
    if (typeof filter.minRating === 'number') {
        const rating = (typeof entry.userRating === 'number') ? entry.userRating : entry.rating;
        if (typeof rating !== 'number' || rating < filter.minRating) return false;
    }
    if (filter.tag) {
        const tags = Array.isArray(entry.tags) ? entry.tags : parseTags(entry.tags);
        const wanted = filter.tag.trim().toLowerCase();
        if (!tags.some(t => String(t).trim().toLowerCase() === wanted)) return false;
    }
    return true;
}

function getActiveSmartGroup() {
    if (!state.activeGroupId) return null;
    const g = state.groups.find(x => x.id === state.activeGroupId);
    return (g && g.filter) ? g : null;
}

function installSmartGroupHook() {
    if (typeof refreshLibrary !== 'function' || refreshLibrary._smartWrapped) return;
    const original = refreshLibrary;
    refreshLibrary = function () {
        const smart = getActiveSmartGroup();
        if (smart) {
            send('getSmartGroupLibrary', { id: smart.id });
        } else {
            original();
        }
    };
    refreshLibrary._smartWrapped = true;
}


const GROUP_COLORS = ['#6366f1', '#f43f5e', '#10b981', '#f59e0b', '#3b82f6', '#8b5cf6', '#ec4899', '#14b8a6',
                      '#ef4444', '#22c55e', '#06b6d4', '#a855f7', '#f97316', '#64748b', '#e11d48', '#0ea5e9'];

function getGroupVnCount(groupId) {
    if (!state.entries) return 0;
    const group = state.groups.find(g => g.id === groupId);
    const filter = parseGroupFilter(group);
    if (filter) return state.entries.filter(e => vnMatchesFilter(e, filter)).length;
    return state.entries.filter(e => e.groupId === groupId).length;
}

const SMART_GROUP_BADGE = `<span class="group-smart-badge">${ICONS.filter}</span>`;

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
        const smartBadge = g.filter ? SMART_GROUP_BADGE : '';
        html += `<div class="group-item${state.activeGroupId === g.id ? ' active' : ''}" data-group="${escapeAttr(g.id)}">
            <span class="group-dot" style="background:${escapeAttr(g.color)}"></span>
            <span class="group-name">${escapeHTML(g.name)}</span>${smartBadge}
            <span class="group-count">${count}</span>
            <button class="group-edit-btn" data-gid="${escapeAttr(g.id)}" title="${t('renameGroup')}">${ICONS.edit}</button>
            <button class="group-color-btn" data-gid="${escapeAttr(g.id)}" title="${t('groupColor')}">
                <span class="group-color-swatch" style="background:${escapeAttr(g.color)}"></span>
            </button>
            <button class="group-delete-btn" data-gid="${escapeAttr(g.id)}" title="${t('deleteGroup')}">${ICONS.close}</button>
        </div>`;
    });

    html += `<div class="group-add" id="groupAddBtn">
        ${ICONS.plus}
        <span>${t('addGroup')}</span>
    </div>`;

    html += `<div class="group-add" id="smartGroupAddBtn">
        ${ICONS.filter}
        <span>${t('addSmartGroup') || 'Add smart group'}</span>
    </div>`;

    container.innerHTML = html;

    container.querySelectorAll('.group-item').forEach(el => {
        el.addEventListener('click', () => {
            const gid = el.dataset.group || null;
            state.activeGroupId = gid;
            renderGroupsSidebar();
            refreshLibrary();
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
                send('updateGroup', { id: group.id, name: newName, color: group.color, filter: group.filter || null, sortOrder: group.sortOrder || 0 });
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
    document.getElementById('smartGroupAddBtn')?.addEventListener('click', promptAddSmartGroup);
}

async function promptAddGroup() {
    const name = await showPromptModal(t('groupName') || 'Group name');
    if (!name) return;

    const color = GROUP_COLORS[state.groups.length % GROUP_COLORS.length];

    send('addGroup', { name, color });
}

async function promptAddSmartGroup() {
    const name = await showPromptModal(t('smartGroupName') || 'Smart group name');
    if (!name) return;

    const statusRaw = await showPromptModal(
        t('filterByStatus') || 'Status: 0=Reading, 1=Completed, 2=On Hold, 3=Dropped, 4=Plan to Read (blank = any)');
    const tagRaw = await showPromptModal(t('filterByTag') || 'Tag (blank = any)');
    const favRaw = await showPromptModal(t('filterByFavorite') || 'Favorites only? yes / no (blank = any)');
    const ratingRaw = await showPromptModal(t('filterByMinRating') || 'Minimum rating (blank = any)');

    const filter = {};

    if (statusRaw != null && statusRaw !== '') {
        const s = parseInt(statusRaw, 10);
        if (Number.isInteger(s) && s >= 0 && s <= 4) filter.status = s;
    }
    if (tagRaw) filter.tag = tagRaw.trim();
    if (favRaw) {
        const v = favRaw.trim().toLowerCase();
        if (v === 'yes' || v === 'y' || v === 'true') filter.isFavorite = true;
        else if (v === 'no' || v === 'n' || v === 'false') filter.isFavorite = false;
    }
    if (ratingRaw != null && ratingRaw !== '') {
        const r = parseFloat(ratingRaw);
        if (!Number.isNaN(r)) filter.minRating = r;
    }

    if (Object.keys(filter).length === 0) {
        showToast(t('smartGroupNoCriteria') || 'A smart group needs at least one criterion', 'warning');
        return;
    }

    const color = GROUP_COLORS[state.groups.length % GROUP_COLORS.length];
    send('addGroup', { name, color, filter: JSON.stringify(filter) });
}

function showGroupColorPicker(anchor, group) {
    document.querySelector('.group-color-picker')?.remove();

    const picker = document.createElement('div');
    picker.className = 'group-color-picker';
    picker.innerHTML = GROUP_COLORS.map(c =>
        `<span class="gcp-swatch${c === group.color ? ' active' : ''}" data-color="${c}" style="background:${c}"></span>`
    ).join('') + `<input type="color" class="gcp-custom" value="${group.color}" title="${t('groupColor')}">`;

    picker.querySelectorAll('.gcp-swatch').forEach(sw => {
        sw.addEventListener('click', (e) => {
            e.stopPropagation();
            send('updateGroup', { id: group.id, name: group.name, color: sw.dataset.color, filter: group.filter || null, sortOrder: group.sortOrder || 0 });
            picker.remove();
        });
    });
    picker.querySelector('.gcp-custom').addEventListener('input', (e) => {
        e.stopPropagation();
        send('updateGroup', { id: group.id, name: group.name, color: e.target.value, filter: group.filter || null, sortOrder: group.sortOrder || 0 });
        picker.remove();
    });

    anchor.parentElement.appendChild(picker);
    setTimeout(() => document.addEventListener('click', () => picker.remove(), { once: true }), 0);
}

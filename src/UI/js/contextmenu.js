let _ctxMenu = null;

function showContextMenu(x, y, entry) {
    hideContextMenu();

    const isRunning = state.runningGames.has(entry.id);
    const statusLabels = getStatusLabels();

    const menu = document.createElement('div');
    menu.className = 'ctx-menu';

    menu.appendChild(ctxItem(
        isRunning ? t('running') : t('launch'),
        '<svg viewBox="0 0 24 24"><polygon points="5 3 19 12 5 21 5 3"/></svg>',
        () => { if (!isRunning) send('launchVn', { id: entry.id }); },
        isRunning ? 'disabled' : ''
    ));

    menu.appendChild(ctxSep());

    menu.appendChild(ctxItem(
        t('favorite') + (entry.isFavorite ? ' ✓' : ''),
        '<svg viewBox="0 0 24 24"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78L12 21.23l8.84-8.84a5.5 5.5 0 0 0 0-7.78z"/></svg>',
        () => { send('toggleFavorite', { id: entry.id }); }
    ));

    menu.appendChild(ctxItem(
        t('pin') + (entry.isPinned ? ' ✓' : ''),
        '<svg viewBox="0 0 24 24"><line x1="12" y1="17" x2="12" y2="22"/><path d="M5 17h14v-1.76a2 2 0 0 0-1.11-1.79l-1.78-.89A2 2 0 0 1 15 10.76V6h1a2 2 0 0 0 0-4H8a2 2 0 0 0 0 4h1v4.76a2 2 0 0 1-1.11 1.79l-1.78.89A2 2 0 0 0 5 15.24z"/></svg>',
        () => { send('togglePin', { id: entry.id }); }
    ));

    menu.appendChild(ctxSep());

    const statusSub = document.createElement('div');
    statusSub.className = 'ctx-submenu';

    const statusTrigger = document.createElement('div');
    statusTrigger.className = 'ctx-menu-item';
    statusTrigger.innerHTML = `
        <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/></svg>
        <span>${t('status')}</span>
        <span style="margin-left:auto;opacity:0.5">▸</span>
    `;
    statusSub.appendChild(statusTrigger);

    const subList = document.createElement('div');
    subList.className = 'ctx-sub-list';
    statusLabels.forEach((label, i) => {
        const item = ctxItem(
            label + (entry.status === i ? ' ✓' : ''),
            '',
            () => { send('setStatus', { id: entry.id, status: i }); }
        );
        subList.appendChild(item);
    });
    statusSub.appendChild(subList);
    menu.appendChild(statusSub);

    if (state.groups && state.groups.length > 0) {
        const groupSub = document.createElement('div');
        groupSub.className = 'ctx-submenu';

        const groupTrigger = document.createElement('div');
        groupTrigger.className = 'ctx-menu-item';
        groupTrigger.innerHTML = `
            <svg viewBox="0 0 24 24"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>
            <span>${t('setGroup')}</span>
            <span style="margin-left:auto;opacity:0.5">▸</span>
        `;
        groupSub.appendChild(groupTrigger);

        const gSubList = document.createElement('div');
        gSubList.className = 'ctx-sub-list';


        gSubList.appendChild(ctxItem(
            t('noGroup') + (!entry.groupId ? ' ✓' : ''),
            '',
            () => { send('setVnGroup', { id: entry.id, groupId: null }); }
        ));

        state.groups.forEach(g => {
            gSubList.appendChild(ctxItem(
                g.name + (entry.groupId === g.id ? ' ✓' : ''),
                `<span class="group-dot" style="background:${escapeAttr(g.color)}"></span>`,
                () => { send('setVnGroup', { id: entry.id, groupId: g.id }); }
            ));
        });

        groupSub.appendChild(gSubList);
        menu.appendChild(groupSub);
    }

    menu.appendChild(ctxSep());

    if (state.settings?.vndbEnabled !== false) {
        menu.appendChild(ctxItem(
            t('refreshMetadata') || 'Refresh Metadata',
            '<svg viewBox="0 0 24 24"><path d="M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8"/><path d="M21 3v5h-5"/><path d="M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16"/><path d="M8 16H3v5"/></svg>',
            () => { send('refreshMetadata', { id: entry.id }); }
        ));
    }

    if (entry.exePath) {
        menu.appendChild(ctxItem(
            t('openFolder'),
            '<svg viewBox="0 0 24 24"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/></svg>',
            () => { send('openFolder', { id: entry.id }); }
        ));
    }

    menu.appendChild(ctxItem(
        t('delete'),
        '<svg viewBox="0 0 24 24"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>',
        () => {
            openDetail(entry);
            document.getElementById('deleteModal').style.display = '';
        },
        'danger'
    ));

    menu.style.left = x + 'px';
    menu.style.top = y + 'px';
    document.body.appendChild(menu);

    const rect = menu.getBoundingClientRect();
    if (rect.right > window.innerWidth) menu.style.left = (x - rect.width) + 'px';
    if (rect.bottom > window.innerHeight) menu.style.top = (y - rect.height) + 'px';

    _ctxMenu = menu;

    setTimeout(() => document.addEventListener('click', hideContextMenu, { once: true }), 0);
}

function hideContextMenu() {
    if (_ctxMenu) {
        _ctxMenu.remove();
        _ctxMenu = null;
    }
}

function ctxItem(label, icon, onClick, cls) {
    const el = document.createElement('div');
    el.className = 'ctx-menu-item' + (cls ? ` ${cls}` : '');
    el.innerHTML = (icon ? icon + ' ' : '') + `<span>${escapeHTML(label)}</span>`;
    el.addEventListener('click', (e) => {
        e.stopPropagation();
        hideContextMenu();
        if (onClick) onClick();
    });
    return el;
}

function ctxSep() {
    const el = document.createElement('div');
    el.className = 'ctx-menu-sep';
    return el;
}

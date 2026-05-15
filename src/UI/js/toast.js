function showToast(message, type = 'info', duration = 3500) {
    let container = document.getElementById('toastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;

    const icons = { success: ICONS.toastSuccess, error: ICONS.toastError, info: ICONS.toastInfo, warning: ICONS.toastWarning };

    toast.innerHTML = `
        <span class="toast-icon">${icons[type] || icons.info}</span>
        <span class="toast-msg">${escapeHTML(message)}</span>
        <button class="toast-close">&times;</button>
    `;

    toast.querySelector('.toast-close').addEventListener('click', () => removeToast(toast));
    container.appendChild(toast);

    requestAnimationFrame(() => toast.classList.add('toast-show'));

    setTimeout(() => removeToast(toast), duration);
}

function removeToast(toast) {
    if (!toast || toast._removing) return;
    toast._removing = true;
    toast.classList.remove('toast-show');
    toast.classList.add('toast-hide');
    setTimeout(() => toast.remove(), 300);
}

function showUndoToast(message, undoCallback, duration = 6000) {
    let container = document.getElementById('toastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = 'toast toast-info';

    toast.innerHTML = `
        <span class="toast-icon">${ICONS.toastInfo}</span>
        <span class="toast-msg">${escapeHTML(message)}</span>
        <button class="toast-undo">${t('undo') || 'Undo'}</button>
        <button class="toast-close">&times;</button>
    `;

    let undone = false;
    toast.querySelector('.toast-undo').addEventListener('click', () => {
        if (!undone) {
            undone = true;
            undoCallback();
            removeToast(toast);
        }
    });
    toast.querySelector('.toast-close').addEventListener('click', () => removeToast(toast));
    container.appendChild(toast);
    requestAnimationFrame(() => toast.classList.add('toast-show'));
    setTimeout(() => { if (!undone) removeToast(toast); }, duration);
}

const CUSTOM_COLOR_DEFAULTS = {
    accent:           { dark: '#2E5FA3', light: '#2E5FA3' },
    accentHover:      { dark: '#3A70B8', light: '#3A70B8' },
    accentSoft:       { dark: '#1A3A6B', light: '#D0DFFF' },
    bgBase:           { dark: '#0D0D1A', light: '#F0F0F5' },
    bgSurface:        { dark: '#13132A', light: '#FFFFFF' },
    bgElevated:       { dark: '#1C1C3A', light: '#E8E8F0' },
    bgHover:          { dark: '#1E1E40', light: '#E0E0EC' },
    borderColor:      { dark: '#20202C', light: '#D0D0E0' },
    textPrimary:      { dark: '#F0F0FF', light: '#1A1A2E' },
    textSecondary:    { dark: '#9090B0', light: '#6060A0' },
    heart:            { dark: '#E05080', light: '#D83060' },
    star:             { dark: '#F0B429', light: '#C89820' },
    danger:           { dark: '#E04050', light: '#D03040' },
    statusReading:    { dark: '#4ADE80', light: '#16A34A' },
    statusCompleted:  { dark: '#60A5FA', light: '#2563EB' },
    statusOnHold:     { dark: '#FACC15', light: '#CA8A04' },
    statusDropped:    { dark: '#F87171', light: '#DC2626' },
    statusPlan:       { dark: '#A78BFA', light: '#7C3AED' },
};

const CUSTOM_COLOR_CSS_VARS = {
    accent:          '--accent',
    accentHover:     '--accent-hover',
    accentSoft:      '--accent-soft',
    bgBase:          '--bg-base',
    bgSurface:       '--bg-surface',
    bgElevated:      '--bg-elevated',
    bgHover:         '--bg-hover',
    borderColor:     '--border',
    textPrimary:     '--text-primary',
    textSecondary:   '--text-secondary',
    heart:           '--heart',
    star:            '--star',
    danger:          '--danger',
    statusReading:   '--status-reading',
    statusCompleted: '--status-completed',
    statusOnHold:    '--status-onhold',
    statusDropped:   '--status-dropped',
    statusPlan:      '--status-plan',
};

const CUSTOM_COLOR_SECTIONS = [
    {
        labelKey: null,
        keys: ['accent', 'accentHover', 'accentSoft', 'bgBase', 'bgSurface', 'bgElevated', 'bgHover', 'borderColor', 'textPrimary', 'textSecondary', 'heart', 'star', 'danger']
    },
    {
        labelKey: 'colorSectionStatuses',
        keys: ['statusReading', 'statusCompleted', 'statusOnHold', 'statusDropped', 'statusPlan']
    }
];

let _customizationSaveTimer = null;

function defaultCustomization() {
    return {
        activeFont: '',
        fonts: [],
        backgroundImage: '',
        backgroundOpacity: 0.4,
        backgroundBlur: 0,
        sidebarBackgroundImage: '',
        sidebarBackgroundOpacity: 0.6,
        sidebarBackgroundBlur: 0,
        topbarBackgroundImage: '',
        topbarBackgroundOpacity: 0.6,
        topbarBackgroundBlur: 0,
        panelSurfaceOpacity: 1.0,
        sidebarWidth: 220,
        cardRadius: 8,
        colors: {}
    };
}

function ensureCustomization() {
    if (!state.settings.customization) {
        state.settings.customization = defaultCustomization();
    }
    const c = state.settings.customization;
    if (!Array.isArray(c.fonts)) c.fonts = [];
    if (!c.colors || typeof c.colors !== 'object') c.colors = {};
    if (typeof c.backgroundOpacity !== 'number') c.backgroundOpacity = 0.4;
    if (typeof c.backgroundBlur !== 'number') c.backgroundBlur = 0;
    if (!c.sidebarBackgroundImage) c.sidebarBackgroundImage = '';
    if (typeof c.sidebarBackgroundOpacity !== 'number') c.sidebarBackgroundOpacity = 0.6;
    if (typeof c.sidebarBackgroundBlur !== 'number') c.sidebarBackgroundBlur = 0;
    if (!c.topbarBackgroundImage) c.topbarBackgroundImage = '';
    if (typeof c.topbarBackgroundOpacity !== 'number') c.topbarBackgroundOpacity = 0.6;
    if (typeof c.topbarBackgroundBlur !== 'number') c.topbarBackgroundBlur = 0;
    if (typeof c.panelSurfaceOpacity !== 'number') c.panelSurfaceOpacity = 1.0;
    if (typeof c.sidebarWidth !== 'number') c.sidebarWidth = 220;
    if (typeof c.cardRadius !== 'number') c.cardRadius = 8;
    return c;
}

function colorDefaultFor(key) {
    const theme = state.settings.theme === 'light' ? 'light' : 'dark';
    return CUSTOM_COLOR_DEFAULTS[key]?.[theme] || '';
}

function applyCustomization() {
    const c = ensureCustomization();
    let style = document.getElementById('custom-theme-style');
    if (!style) {
        style = document.createElement('style');
        style.id = 'custom-theme-style';
        document.head.appendChild(style);
    }

    let css = '';

    if (c.activeFont) {
        const url = 'https://fonts.vnhub.local/' + encodeURIComponent(c.activeFont);
        css += `@font-face { font-family: 'VnHubUserFont'; src: url('${url}'); font-display: swap; }\n`;
        css += `:root { --font-ui: 'VnHubUserFont', 'Inter', system-ui, sans-serif; }\n`;
    }

    const colorEntries = Object.entries(c.colors).filter(([_, v]) => v);
    if (colorEntries.length) {
        css += ':root, :root[data-theme="light"] {\n';
        for (const [key, val] of colorEntries) {
            const cssVar = CUSTOM_COLOR_CSS_VARS[key];
            if (cssVar && /^#[0-9a-fA-F]{6}$/.test(val)) {
                css += `  ${cssVar}: ${val};\n`;
            }
        }
        css += '}\n';
    }

    style.textContent = css;

    const root = document.documentElement;
    if (c.backgroundImage) {
        const bgUrl = 'https://bg.vnhub.local/' + encodeURIComponent(c.backgroundImage);
        root.style.setProperty('--custom-bg-image', `url('${bgUrl}')`);
        root.style.setProperty('--custom-bg-opacity', String(c.backgroundOpacity));
        root.style.setProperty('--custom-bg-blur', `${c.backgroundBlur}px`);
    } else {
        root.style.removeProperty('--custom-bg-image');
        root.style.setProperty('--custom-bg-opacity', '0');
        root.style.setProperty('--custom-bg-blur', '0px');
    }

    if (c.sidebarBackgroundImage) {
        const url = 'https://bg.vnhub.local/' + encodeURIComponent(c.sidebarBackgroundImage);
        root.style.setProperty('--sidebar-bg-image', `url('${url}')`);
        root.style.setProperty('--sidebar-bg-opacity', String(c.sidebarBackgroundOpacity));
        root.style.setProperty('--sidebar-bg-blur', `${c.sidebarBackgroundBlur}px`);
    } else {
        root.style.removeProperty('--sidebar-bg-image');
        root.style.setProperty('--sidebar-bg-opacity', '0');
        root.style.setProperty('--sidebar-bg-blur', '0px');
    }

    if (c.topbarBackgroundImage) {
        const url = 'https://bg.vnhub.local/' + encodeURIComponent(c.topbarBackgroundImage);
        root.style.setProperty('--topbar-bg-image', `url('${url}')`);
        root.style.setProperty('--topbar-bg-opacity', String(c.topbarBackgroundOpacity));
        root.style.setProperty('--topbar-bg-blur', `${c.topbarBackgroundBlur}px`);
    } else {
        root.style.removeProperty('--topbar-bg-image');
        root.style.setProperty('--topbar-bg-opacity', '0');
        root.style.setProperty('--topbar-bg-blur', '0px');
    }

    root.style.setProperty('--panel-surface-opacity', String(c.panelSurfaceOpacity));
    root.dataset.sidebarConfiguredWidth = `${c.sidebarWidth}px`;
    if (!document.getElementById('sidebar').classList.contains('collapsed')) {
        root.style.setProperty('--sidebar-width', `${c.sidebarWidth}px`);
    }
    root.style.setProperty('--radius', `${c.cardRadius}px`);
    root.style.setProperty('--radius-lg', `${Math.round(c.cardRadius * 1.5)}px`);
}

function debouncedSaveCustomization() {
    if (_customizationSaveTimer) clearTimeout(_customizationSaveTimer);
    _customizationSaveTimer = setTimeout(() => {
        send('saveSettings', state.settings);
    }, 250);
}

function renderCustomFontList() {
    const container = document.getElementById('customFontList');
    if (!container) return;
    const c = ensureCustomization();
    if (!c.fonts.length) {
        container.innerHTML = `<div class="custom-font-empty">${t('noFonts')}</div>`;
        return;
    }
    container.innerHTML = c.fonts.map(name => {
        const active = name === c.activeFont;
        const safe = escapeHTML(name);
        return `<div class="custom-font-item ${active ? 'active' : ''}">
            <input type="radio" name="customFontRadio" data-font="${safe}" ${active ? 'checked' : ''}>
            <span class="custom-font-item-label" title="${safe}">${safe}</span>
            <button type="button" class="custom-font-item-remove" data-remove-font="${safe}" title="${t('delete')}">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6"/><path d="M14 11v6"/></svg>
            </button>
        </div>`;
    }).join('');

    container.querySelectorAll('input[type="radio"]').forEach(r => {
        r.addEventListener('change', () => {
            c.activeFont = r.dataset.font;
            applyCustomization();
            renderCustomFontList();
            debouncedSaveCustomization();
        });
    });
    container.querySelectorAll('[data-remove-font]').forEach(btn => {
        btn.addEventListener('click', () => {
            const name = btn.dataset.removeFont;
            send('removeFont', { fileName: name });
        });
    });
}

function renderCustomBackground() {
    const c = ensureCustomization();
    const preview = document.getElementById('customBackgroundPreview');
    if (preview) {
        if (c.backgroundImage) {
            const url = 'https://bg.vnhub.local/' + encodeURIComponent(c.backgroundImage);
            preview.style.backgroundImage = `url('${url}')`;
            preview.textContent = '';
        } else {
            preview.style.backgroundImage = '';
            preview.textContent = t('noFonts') ? '' : '';
        }
    }
    const op = document.getElementById('settingsBgOpacity');
    const opV = document.getElementById('bgOpacityValue');
    if (op) op.value = Math.round(c.backgroundOpacity * 100);
    if (opV) opV.textContent = Math.round(c.backgroundOpacity * 100) + '%';

    const bl = document.getElementById('settingsBgBlur');
    const blV = document.getElementById('bgBlurValue');
    if (bl) bl.value = c.backgroundBlur;
    if (blV) blV.textContent = c.backgroundBlur + 'px';
}

function renderCustomColors() {
    const grid = document.getElementById('customColorGrid');
    if (!grid) return;
    const c = ensureCustomization();

    grid.innerHTML = CUSTOM_COLOR_SECTIONS.map(section => {
        const header = section.labelKey
            ? `<div class="custom-color-section-label">${t(section.labelKey)}</div>`
            : '';
        const items = section.keys.map(key => {
            const labelKey = 'color' + key.charAt(0).toUpperCase() + key.slice(1);
            const value = c.colors[key] || colorDefaultFor(key);
            return `<label class="custom-color-item">
            <input type="color" data-color-key="${key}" value="${value}">
            <span class="custom-color-item-label">${t(labelKey) || key}</span>
            <button type="button" class="custom-color-item-reset" data-reset-color="${key}" title="${t('resetSection')}">&times;</button>
        </label>`;
        }).join('');
        return `${header}<div class="custom-color-section">${items}</div>`;
    }).join('');

    grid.querySelectorAll('input[type="color"]').forEach(inp => {
        inp.addEventListener('input', () => {
            const key = inp.dataset.colorKey;
            ensureCustomization().colors[key] = inp.value;
            applyCustomization();
            debouncedSaveCustomization();
        });
    });
    grid.querySelectorAll('[data-reset-color]').forEach(btn => {
        btn.addEventListener('click', () => {
            const key = btn.dataset.resetColor;
            delete ensureCustomization().colors[key];
            applyCustomization();
            renderCustomColors();
            debouncedSaveCustomization();
        });
    });
}

function renderPanelBackground(panel) {
    const c = ensureCustomization();
    const cap = panel.charAt(0).toUpperCase() + panel.slice(1);
    const img = c[panel + 'BackgroundImage'];

    const preview = document.getElementById('custom' + cap + 'BgPreview');
    if (preview) {
        preview.style.backgroundImage = img
            ? `url('https://bg.vnhub.local/${encodeURIComponent(img)}')`
            : '';
    }

    const op = document.getElementById('settings' + cap + 'BgOpacity');
    const opV = document.getElementById(panel + 'BgOpacityValue');
    const opVal = c[panel + 'BackgroundOpacity'];
    if (op) op.value = Math.round(opVal * 100);
    if (opV) opV.textContent = Math.round(opVal * 100) + '%';

    const bl = document.getElementById('settings' + cap + 'BgBlur');
    const blV = document.getElementById(panel + 'BgBlurValue');
    const blVal = c[panel + 'BackgroundBlur'];
    if (bl) bl.value = blVal;
    if (blV) blV.textContent = blVal + 'px';
}

function renderSidebarBackground() { renderPanelBackground('sidebar'); }
function renderTopbarBackground() { renderPanelBackground('topbar'); }

function renderPanelStyle() {
    const c = ensureCustomization();
    const op = document.getElementById('settingsPanelSurfaceOpacity');
    const opV = document.getElementById('panelSurfaceOpacityValue');
    if (op) op.value = Math.round(c.panelSurfaceOpacity * 100);
    if (opV) opV.textContent = Math.round(c.panelSurfaceOpacity * 100) + '%';
}

function renderLayoutSliders() {
    const c = ensureCustomization();
    const sw = document.getElementById('settingsSidebarWidth');
    const swV = document.getElementById('sidebarWidthValue');
    if (sw) sw.value = c.sidebarWidth;
    if (swV) swV.textContent = c.sidebarWidth + 'px';

    const cr = document.getElementById('settingsCardRadius');
    const crV = document.getElementById('cardRadiusValue');
    if (cr) cr.value = c.cardRadius;
    if (crV) crV.textContent = c.cardRadius + 'px';
}

function renderCustomization() {
    renderCustomFontList();
    renderCustomBackground();
    renderSidebarBackground();
    renderTopbarBackground();
    renderPanelStyle();
    renderLayoutSliders();
    renderCustomColors();
}

function resetCustomizationSection(section) {
    const c = ensureCustomization();
    if (section === 'fonts') {
        c.activeFont = '';
    } else if (section === 'background') {
        if (c.backgroundImage) send('clearBackground');
        c.backgroundImage = '';
        c.backgroundOpacity = 0.4;
        c.backgroundBlur = 0;
    } else if (section === 'sidebar') {
        if (c.sidebarBackgroundImage) send('clearSidebarBackground');
        c.sidebarBackgroundImage = '';
        c.sidebarBackgroundOpacity = 0.6;
        c.sidebarBackgroundBlur = 0;
    } else if (section === 'topbar') {
        if (c.topbarBackgroundImage) send('clearTopbarBackground');
        c.topbarBackgroundImage = '';
        c.topbarBackgroundOpacity = 0.6;
        c.topbarBackgroundBlur = 0;
    } else if (section === 'panelStyle') {
        c.panelSurfaceOpacity = 1.0;
    } else if (section === 'layout') {
        c.sidebarWidth = 220;
        c.cardRadius = 8;
    } else if (section === 'colors') {
        c.colors = {};
    }
    applyCustomization();
    renderCustomization();
    debouncedSaveCustomization();
}

function initCustomization() {
    const addBtn = document.getElementById('btnAddFont');
    if (addBtn && !addBtn.dataset.bound) { addBtn.dataset.bound = '1'; addBtn.addEventListener('click', () => send('pickFont')); }

    const pickBg = document.getElementById('btnPickBackground');
    if (pickBg && !pickBg.dataset.bound) { pickBg.dataset.bound = '1'; pickBg.addEventListener('click', () => send('pickBackground')); }

    const clearBg = document.getElementById('btnClearBackground');
    if (clearBg && !clearBg.dataset.bound) {
        clearBg.dataset.bound = '1';
        clearBg.addEventListener('click', () => {
            ensureCustomization().backgroundImage = '';
            send('clearBackground');
        });
    }

    const op = document.getElementById('settingsBgOpacity');
    if (op && !op.dataset.bound) {
        op.dataset.bound = '1';
        op.addEventListener('input', e => {
            const v = parseInt(e.target.value, 10) / 100;
            ensureCustomization().backgroundOpacity = v;
            document.getElementById('bgOpacityValue').textContent = e.target.value + '%';
            applyCustomization();
            debouncedSaveCustomization();
        });
    }

    const bl = document.getElementById('settingsBgBlur');
    if (bl && !bl.dataset.bound) {
        bl.dataset.bound = '1';
        bl.addEventListener('input', e => {
            const v = parseInt(e.target.value, 10);
            ensureCustomization().backgroundBlur = v;
            document.getElementById('bgBlurValue').textContent = v + 'px';
            applyCustomization();
            debouncedSaveCustomization();
        });
    }

    const pickSidebar = document.getElementById('btnPickSidebarBackground');
    if (pickSidebar && !pickSidebar.dataset.bound) { pickSidebar.dataset.bound = '1'; pickSidebar.addEventListener('click', () => send('pickSidebarBackground')); }

    const clearSidebar = document.getElementById('btnClearSidebarBackground');
    if (clearSidebar && !clearSidebar.dataset.bound) {
        clearSidebar.dataset.bound = '1';
        clearSidebar.addEventListener('click', () => {
            ensureCustomization().sidebarBackgroundImage = '';
            send('clearSidebarBackground');
        });
    }

    const sidebarOp = document.getElementById('settingsSidebarBgOpacity');
    if (sidebarOp && !sidebarOp.dataset.bound) {
        sidebarOp.dataset.bound = '1';
        sidebarOp.addEventListener('input', e => {
            const v = parseInt(e.target.value, 10) / 100;
            ensureCustomization().sidebarBackgroundOpacity = v;
            document.getElementById('sidebarBgOpacityValue').textContent = e.target.value + '%';
            applyCustomization();
            debouncedSaveCustomization();
        });
    }

    const sidebarBl = document.getElementById('settingsSidebarBgBlur');
    if (sidebarBl && !sidebarBl.dataset.bound) {
        sidebarBl.dataset.bound = '1';
        sidebarBl.addEventListener('input', e => {
            const v = parseInt(e.target.value, 10);
            ensureCustomization().sidebarBackgroundBlur = v;
            document.getElementById('sidebarBgBlurValue').textContent = v + 'px';
            applyCustomization();
            debouncedSaveCustomization();
        });
    }

    const pickTopbar = document.getElementById('btnPickTopbarBackground');
    if (pickTopbar && !pickTopbar.dataset.bound) { pickTopbar.dataset.bound = '1'; pickTopbar.addEventListener('click', () => send('pickTopbarBackground')); }

    const clearTopbar = document.getElementById('btnClearTopbarBackground');
    if (clearTopbar && !clearTopbar.dataset.bound) {
        clearTopbar.dataset.bound = '1';
        clearTopbar.addEventListener('click', () => {
            ensureCustomization().topbarBackgroundImage = '';
            send('clearTopbarBackground');
        });
    }

    const topbarOp = document.getElementById('settingsTopbarBgOpacity');
    if (topbarOp && !topbarOp.dataset.bound) {
        topbarOp.dataset.bound = '1';
        topbarOp.addEventListener('input', e => {
            const v = parseInt(e.target.value, 10) / 100;
            ensureCustomization().topbarBackgroundOpacity = v;
            document.getElementById('topbarBgOpacityValue').textContent = e.target.value + '%';
            applyCustomization();
            debouncedSaveCustomization();
        });
    }

    const topbarBl = document.getElementById('settingsTopbarBgBlur');
    if (topbarBl && !topbarBl.dataset.bound) {
        topbarBl.dataset.bound = '1';
        topbarBl.addEventListener('input', e => {
            const v = parseInt(e.target.value, 10);
            ensureCustomization().topbarBackgroundBlur = v;
            document.getElementById('topbarBgBlurValue').textContent = v + 'px';
            applyCustomization();
            debouncedSaveCustomization();
        });
    }

    const panelOp = document.getElementById('settingsPanelSurfaceOpacity');
    if (panelOp && !panelOp.dataset.bound) {
        panelOp.dataset.bound = '1';
        panelOp.addEventListener('input', e => {
            const v = parseInt(e.target.value, 10) / 100;
            ensureCustomization().panelSurfaceOpacity = v;
            document.getElementById('panelSurfaceOpacityValue').textContent = e.target.value + '%';
            applyCustomization();
            debouncedSaveCustomization();
        });
    }

    const sidebarW = document.getElementById('settingsSidebarWidth');
    if (sidebarW && !sidebarW.dataset.bound) {
        sidebarW.dataset.bound = '1';
        sidebarW.addEventListener('input', e => {
            const v = parseInt(e.target.value, 10);
            ensureCustomization().sidebarWidth = v;
            document.getElementById('sidebarWidthValue').textContent = v + 'px';
            applyCustomization();
            debouncedSaveCustomization();
        });
    }

    const cardR = document.getElementById('settingsCardRadius');
    if (cardR && !cardR.dataset.bound) {
        cardR.dataset.bound = '1';
        cardR.addEventListener('input', e => {
            const v = parseInt(e.target.value, 10);
            ensureCustomization().cardRadius = v;
            document.getElementById('cardRadiusValue').textContent = v + 'px';
            applyCustomization();
            debouncedSaveCustomization();
        });
    }

    document.querySelectorAll('[data-customization-reset]').forEach(btn => {
        if (!btn.dataset.bound) {
            btn.dataset.bound = '1';
            btn.addEventListener('click', () => {
                resetCustomizationSection(btn.dataset.customizationReset);
            });
        }
    });

    renderCustomization();
}


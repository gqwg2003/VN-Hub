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

function applyBgVars(root, prefix, image, opacity, blur) {
    if (image) {
        const url = 'https://bg.vnhub.local/' + encodeURIComponent(image);
        root.style.setProperty(`--${prefix}-image`, `url('${url}')`);
        root.style.setProperty(`--${prefix}-opacity`, String(opacity));
        root.style.setProperty(`--${prefix}-blur`, `${blur}px`);
    } else {
        root.style.removeProperty(`--${prefix}-image`);
        root.style.setProperty(`--${prefix}-opacity`, '0');
        root.style.setProperty(`--${prefix}-blur`, '0px');
    }
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
    applyBgVars(root, 'custom-bg', c.backgroundImage, c.backgroundOpacity, c.backgroundBlur);
    applyBgVars(root, 'sidebar-bg', c.sidebarBackgroundImage, c.sidebarBackgroundOpacity, c.sidebarBackgroundBlur);
    applyBgVars(root, 'topbar-bg', c.topbarBackgroundImage, c.topbarBackgroundOpacity, c.topbarBackgroundBlur);

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
                ${ICONS.trash}
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

function bindOnce(el, ev, fn) {
    if (el && !el.dataset.bound) {
        el.dataset.bound = '1';
        el.addEventListener(ev, fn);
    }
}

function bindCustomPick(btnId, action) {
    bindOnce(document.getElementById(btnId), 'click', () => send(action));
}

function bindCustomClear(btnId, prop, action) {
    bindOnce(document.getElementById(btnId), 'click', () => {
        ensureCustomization()[prop] = '';
        send(action);
    });
}

// suffix '%' stores value/100 and displays the raw slider value;
// suffix 'px' stores the parsed value and displays it with the px unit.
function bindCustomSlider(inputId, valueId, prop, suffix) {
    bindOnce(document.getElementById(inputId), 'input', e => {
        const raw = parseInt(e.target.value, 10);
        const c = ensureCustomization();
        if (suffix === '%') {
            c[prop] = raw / 100;
            document.getElementById(valueId).textContent = e.target.value + '%';
        } else {
            c[prop] = raw;
            document.getElementById(valueId).textContent = raw + 'px';
        }
        applyCustomization();
        debouncedSaveCustomization();
    });
}

function initCustomization() {
    bindCustomPick('btnAddFont', 'pickFont');

    bindCustomPick('btnPickBackground', 'pickBackground');
    bindCustomClear('btnClearBackground', 'backgroundImage', 'clearBackground');
    bindCustomSlider('settingsBgOpacity', 'bgOpacityValue', 'backgroundOpacity', '%');
    bindCustomSlider('settingsBgBlur', 'bgBlurValue', 'backgroundBlur', 'px');

    bindCustomPick('btnPickSidebarBackground', 'pickSidebarBackground');
    bindCustomClear('btnClearSidebarBackground', 'sidebarBackgroundImage', 'clearSidebarBackground');
    bindCustomSlider('settingsSidebarBgOpacity', 'sidebarBgOpacityValue', 'sidebarBackgroundOpacity', '%');
    bindCustomSlider('settingsSidebarBgBlur', 'sidebarBgBlurValue', 'sidebarBackgroundBlur', 'px');

    bindCustomPick('btnPickTopbarBackground', 'pickTopbarBackground');
    bindCustomClear('btnClearTopbarBackground', 'topbarBackgroundImage', 'clearTopbarBackground');
    bindCustomSlider('settingsTopbarBgOpacity', 'topbarBgOpacityValue', 'topbarBackgroundOpacity', '%');
    bindCustomSlider('settingsTopbarBgBlur', 'topbarBgBlurValue', 'topbarBackgroundBlur', 'px');

    bindCustomSlider('settingsPanelSurfaceOpacity', 'panelSurfaceOpacityValue', 'panelSurfaceOpacity', '%');
    bindCustomSlider('settingsSidebarWidth', 'sidebarWidthValue', 'sidebarWidth', 'px');
    bindCustomSlider('settingsCardRadius', 'cardRadiusValue', 'cardRadius', 'px');

    document.querySelectorAll('[data-customization-reset]').forEach(btn => {
        bindOnce(btn, 'click', () => resetCustomizationSection(btn.dataset.customizationReset));
    });

    renderCustomization();
}


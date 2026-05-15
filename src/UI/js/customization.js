const CUSTOM_COLOR_DEFAULTS = {
    accent: { dark: '#2E5FA3', light: '#2E5FA3' },
    accentHover: { dark: '#3A70B8', light: '#3A70B8' },
    accentSoft: { dark: '#1A3A6B', light: '#D0DFFF' },
    bgSurface: { dark: '#13132A', light: '#FFFFFF' },
    bgElevated: { dark: '#1C1C3A', light: '#E8E8F0' },
    bgHover: { dark: '#1E1E40', light: '#E0E0EC' },
    heart: { dark: '#E05080', light: '#D83060' },
    star: { dark: '#F0B429', light: '#C89820' },
    danger: { dark: '#E04050', light: '#D03040' }
};

const CUSTOM_COLOR_CSS_VARS = {
    accent: '--accent',
    accentHover: '--accent-hover',
    accentSoft: '--accent-soft',
    bgSurface: '--bg-surface',
    bgElevated: '--bg-elevated',
    bgHover: '--bg-hover',
    heart: '--heart',
    star: '--star',
    danger: '--danger'
};

let _customizationSaveTimer = null;

function defaultCustomization() {
    return {
        activeFont: '',
        fonts: [],
        backgroundImage: '',
        backgroundOpacity: 0.4,
        backgroundBlur: 0,
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
        css += ':root {\n';
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
        const safe = escapeHtml(name);
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
    grid.innerHTML = Object.keys(CUSTOM_COLOR_DEFAULTS).map(key => {
        const labelKey = 'color' + key.charAt(0).toUpperCase() + key.slice(1);
        const value = c.colors[key] || colorDefaultFor(key);
        return `<label class="custom-color-item">
            <input type="color" data-color-key="${key}" value="${value}">
            <span class="custom-color-item-label">${t(labelKey) || key}</span>
            <button type="button" class="custom-color-item-reset" data-reset-color="${key}" title="${t('resetSection')}">&times;</button>
        </label>`;
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

function renderCustomization() {
    renderCustomFontList();
    renderCustomBackground();
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
    } else if (section === 'colors') {
        c.colors = {};
    }
    applyCustomization();
    renderCustomization();
    debouncedSaveCustomization();
}

function initCustomization() {
    const addBtn = document.getElementById('btnAddFont');
    if (addBtn) addBtn.addEventListener('click', () => send('pickFont'));

    const pickBg = document.getElementById('btnPickBackground');
    if (pickBg) pickBg.addEventListener('click', () => send('pickBackground'));

    const clearBg = document.getElementById('btnClearBackground');
    if (clearBg) clearBg.addEventListener('click', () => {
        ensureCustomization().backgroundImage = '';
        send('clearBackground');
    });

    const op = document.getElementById('settingsBgOpacity');
    if (op) op.addEventListener('input', e => {
        const v = parseInt(e.target.value, 10) / 100;
        ensureCustomization().backgroundOpacity = v;
        document.getElementById('bgOpacityValue').textContent = e.target.value + '%';
        applyCustomization();
        debouncedSaveCustomization();
    });

    const bl = document.getElementById('settingsBgBlur');
    if (bl) bl.addEventListener('input', e => {
        const v = parseInt(e.target.value, 10);
        ensureCustomization().backgroundBlur = v;
        document.getElementById('bgBlurValue').textContent = v + 'px';
        applyCustomization();
        debouncedSaveCustomization();
    });

    document.querySelectorAll('[data-customization-reset]').forEach(btn => {
        btn.addEventListener('click', () => {
            resetCustomizationSection(btn.dataset.customizationReset);
        });
    });

    renderCustomization();
}

function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, c => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    }[c]));
}

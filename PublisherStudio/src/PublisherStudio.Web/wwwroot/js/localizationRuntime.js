(() => {
    'use strict';
    const requestedLanguage = String(document.documentElement.lang || navigator.language || 'en-US');
    const neutral = requestedLanguage.toLowerCase().split('-')[0];
    const devExtremeCultures = new Set(['ar','bg','ca','cs','da','de','el','en','es','fa','fi','fr','hu','it','ja','ko','lt','lv','nb','nl','pl','pt','ro','ru','sk','sl','sv','tr','uk','vi','zh']);
    const excludedSelector = 'script,style,code,pre,textarea,[contenteditable="true"],.print-publication,[data-publication-element],.publication-content-source,.text-frame-content,.spreadsheet-preview-html';
    let dictionary = {};
    let sourceMap = new Map();
    let phrases = [];
    let words = new Map();
    let observer = null;
    let applying = false;

    function normalize(value) { return String(value || '').replace(/\s+/g, ' ').trim(); }
    function rebuildSourceMap() {
        sourceMap = new Map(); phrases = []; words = new Map();
        for (const [key, value] of Object.entries(dictionary || {})) {
            if (key.startsWith('Text.')) {
                const source = key.slice(5).replaceAll('␠', ' ');
                if (source) sourceMap.set(normalize(source), String(value ?? ''));
            } else if (key.startsWith('Phrase.')) {
                phrases.push([key.slice(7).replaceAll('␠', ' '), String(value ?? '')]);
            } else if (key.startsWith('Word.')) {
                words.set(key.slice(5).toLowerCase(), String(value ?? ''));
            }
        }
        phrases.sort((left, right) => right[0].length - left[0].length);
    }
    function preserveCase(source, target) {
        if (!target) return target;
        if (source.toUpperCase() === source) return target.toUpperCase();
        if (source[0] && source[0].toUpperCase() === source[0]) return target[0]?.toUpperCase() + target.slice(1);
        return target;
    }
    function fallbackTranslate(value) {
        if (neutral !== 'de') return value;
        let result = value;
        for (const [source, replacement] of phrases) {
            const pattern = new RegExp(source.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi');
            result = result.replace(pattern, match => preserveCase(match, replacement));
        }

        // Never create a half German / half English label. Word fallback is accepted only
        // when every ordinary word is known. Product names, acronyms and protocol tokens
        // intentionally remain invariant.
        const invariant = /^(?:AI|API|CSS|DX|DXAIChat|DIV|HTML|HTTP|HTTPS|JSON|MFA|OCR|REST|SQL|SQLite|UI|URL|GPU|CPU|LocalGPT|PublisherStudio|Blazor|DevExpress|DevExtreme|OData|Ollama|LM|Studio|Wire)$/i;
        let complete = true;
        const translated = result.replace(/\b[A-Za-z][A-Za-z'-]*\b/g, token => {
            const replacement = words.get(token.toLowerCase());
            if (replacement) return preserveCase(token, replacement);
            if (invariant.test(token) || token.length <= 2) return token;
            complete = false;
            return token;
        });
        return complete ? translated : value;
    }
    function translated(value) {
        const original = normalize(value);
        return sourceMap.get(original) || fallbackTranslate(original) || original;
    }
    function isExcluded(node) {
        const element = node instanceof Element ? node : node?.parentElement;
        return !element || Boolean(element.closest(excludedSelector));
    }
    function translateTextNode(node) {
        if (!(node instanceof Text) || isExcluded(node)) return;
        const original = normalize(node.nodeValue);
        if (!original) return;
        const replacement = translated(original);
        if (!replacement || replacement === original) return;
        const leading = /^\s*/.exec(node.nodeValue)?.[0] || '';
        const trailing = /\s*$/.exec(node.nodeValue)?.[0] || '';
        node.nodeValue = `${leading}${replacement}${trailing}`;
    }
    function translateElement(element) {
        if (!(element instanceof Element) || isExcluded(element)) return;
        const explicitKey = element.getAttribute('data-i18n-key');
        if (explicitKey) {
            const explicit = dictionary?.[explicitKey] ?? dictionary?.[`Text.${explicitKey}`];
            if (explicit !== undefined && element.getAttribute('data-i18n-target') !== 'attribute') element.textContent = String(explicit);
        }
        for (const attribute of ['title', 'aria-label', 'placeholder']) {
            const value = element.getAttribute(attribute);
            if (!value) continue;
            const replacement = translated(value);
            if (replacement && replacement !== value) element.setAttribute(attribute, replacement);
        }
        if (element.matches('input[type="button"],input[type="submit"],input[type="reset"]')) {
            const replacement = translated(element.value);
            if (replacement && replacement !== element.value) element.value = replacement;
        }
        for (const node of element.childNodes) if (node instanceof Text) translateTextNode(node);
    }
    function apply(root = document.body) {
        if (!root || applying) return;
        applying = true;
        try {
            if (root instanceof Text) translateTextNode(root);
            else if (root instanceof Element || root instanceof Document) {
                if (root instanceof Element) translateElement(root);
                root.querySelectorAll?.('button,label,option,summary,h1,h2,h3,h4,p,span,strong,small,input,select,[title],[aria-label],[placeholder],[data-i18n-key]').forEach(translateElement);
            }
        } finally { applying = false; }
    }
    async function load(culture = requestedLanguage) {
        try {
            const response = await fetch(`/api/configuration/localization/${encodeURIComponent(culture)}`, { cache: 'no-store' });
            if (response.ok) dictionary = await response.json();
        } catch (error) { console.warn('PublisherStudio localization dictionary could not be loaded.', error); }
        rebuildSourceMap();
        document.title = translated(document.title);
        apply(document.body);
        observer?.disconnect();
        observer = new MutationObserver(records => {
            if (applying) return;
            for (const record of records) {
                record.addedNodes.forEach(apply);
                if (record.type === 'attributes') translateElement(record.target);
            }
        });
        observer.observe(document.documentElement, { subtree: true, childList: true, attributes: true, attributeFilter: ['title','aria-label','placeholder'] });
    }
    function loadDevExtreme() {
        const locale = devExtremeCultures.has(neutral) ? neutral : 'en';
        if (locale === 'en') { globalThis.DevExpress?.localization?.locale?.('en'); return; }
        const script = document.createElement('script');
        script.src = `vendor/devextreme-dist/js/localization/dx.messages.${locale}.js`;
        script.defer = false;
        script.onload = () => globalThis.DevExpress?.localization?.locale?.(requestedLanguage.toLowerCase());
        document.head.appendChild(script);
    }
    window.PublisherStudioLocalization = {
        refresh: apply,
        dictionary: () => ({ ...dictionary }),
        async setCulture(culture, returnUrl = location.pathname + location.search) {
            location.assign(`/api/configuration/localization/select?culture=${encodeURIComponent(culture)}&returnUrl=${encodeURIComponent(returnUrl || '/')}`);
        }
    };
    loadDevExtreme();
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => load(), { once: true });
    else void load();
})();

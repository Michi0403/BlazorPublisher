(() => {
    const language = String(document.documentElement.lang || navigator.language || 'en').toLowerCase();
    const neutral = language.split('-')[0];
    const supported = new Set(['ar','bg','ca','cs','da','de','el','en','es','fa','fi','fr','hu','it','ja','ko','lt','lv','nb','nl','pl','pt','ro','ru','sk','sl','sv','tr','uk','vi','zh']);
    const locale = supported.has(neutral) ? neutral : 'en';
    if (locale !== 'en') {
        const script = document.createElement('script');
        script.src = `vendor/devextreme-dist/js/localization/dx.messages.${locale}.js`;
        script.defer = false;
        script.onload = () => globalThis.DevExpress?.localization?.locale?.(language);
        document.head.appendChild(script);
    } else globalThis.DevExpress?.localization?.locale?.('en');
})();

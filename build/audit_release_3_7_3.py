#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import re
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CSS = ROOT / 'docs/templates/publisherstudio/public/main.css'
JS = ROOT / 'docs/templates/publisherstudio/public/main.js'
COPIES = [
    ROOT / 'src/PublisherStudio.Web/wwwroot/help-docs/public/main.css',
    ROOT / 'src/PublisherStudio.Web/wwwroot/help-docs/styles/publisherstudio-kawaii.css',
    ROOT / 'src/PublisherStudio.Web/wwwroot/help-docs/public/main.js',
    ROOT / 'src/PublisherStudio.Web/wwwroot/help-docs/styles/publisherstudio-kawaii.js',
]
SNAPSHOT = ROOT / '.github/pages/publisherstudio-kawaii-docs.zip'


def fail(message: str) -> None:
    raise SystemExit(f'PublisherStudio 3.7.3 audit failed: {message}')


def main() -> int:
    project = (ROOT / 'src/PublisherStudio.Web/PublisherStudio.Web.csproj').read_text(encoding='utf-8')
    if '<Version>3.7.3</Version>' not in project:
        fail('project version is not 3.7.3')

    css = CSS.read_text(encoding='utf-8')
    js = JS.read_text(encoding='utf-8')
    required_css = (
        '.publisherstudio-pointer-overlay {',
        'contain: strict;',
        'overflow: clip;',
        'position: fixed !important;',
        'body > :not(.publisherstudio-kawaii-sky):not(.publisherstudio-pointer-overlay) { position: relative; z-index: 2; }',
    )
    for marker in required_css:
        if marker not in css:
            fail(f'missing CSS marker {marker!r}')
    required_js = (
        'function ensureKawaiiPointerOverlay()',
        'overlay.appendChild(paw);',
        'overlay.appendChild(trail);',
        'overlay.appendChild(sparkle);',
        'overlay.appendChild(scratch);',
        'overlay.appendChild(pop);',
    )
    for marker in required_js:
        if marker not in js:
            fail(f'missing JS marker {marker!r}')
    for marker in ('document.body.appendChild(paw);','document.body.appendChild(trail);','document.body.appendChild(sparkle);','document.body.appendChild(scratch);','document.body.appendChild(pop);'):
        if marker in js:
            fail(f'transient pointer effect still escapes overlay: {marker}')

    css_bytes = CSS.read_bytes()
    js_bytes = JS.read_bytes()
    for path in COPIES[:2]:
        if path.read_bytes() != css_bytes:
            fail(f'{path.relative_to(ROOT)} differs from maintained CSS')
    for path in COPIES[2:]:
        if path.read_bytes() != js_bytes:
            fail(f'{path.relative_to(ROOT)} differs from maintained JavaScript')

    css_hash = hashlib.sha256(css_bytes).hexdigest()[:12]
    js_hash = hashlib.sha256(js_bytes).hexdigest()[:12]
    help_root = ROOT / 'src/PublisherStudio.Web/wwwroot/help-docs'
    for html in help_root.rglob('*.html'):
        text = html.read_text(encoding='utf-8', errors='ignore')
        if 'publisherstudio-kawaii.css?v=' in text and f'publisherstudio-kawaii.css?v={css_hash}' not in text:
            fail(f'stale CSS cache key in {html.relative_to(ROOT)}')
        if 'publisherstudio-kawaii.js?v=' in text and f'publisherstudio-kawaii.js?v={js_hash}' not in text:
            fail(f'stale JS cache key in {html.relative_to(ROOT)}')

    with zipfile.ZipFile(SNAPSHOT) as archive:
        if archive.testzip() is not None:
            fail('tracked Pages snapshot is corrupt')
        if archive.read('styles/publisherstudio-kawaii.css') != css_bytes or archive.read('public/main.css') != css_bytes:
            fail('tracked Pages snapshot CSS is not synchronized')
        if archive.read('styles/publisherstudio-kawaii.js') != js_bytes or archive.read('public/main.js') != js_bytes:
            fail('tracked Pages snapshot JavaScript is not synchronized')

    print('PublisherStudio 3.7.3 audit passed: pointer decorations are viewport-contained, scroll-neutral, and synchronized across documentation assets.')
    return 0


if __name__ == '__main__':
    sys.exit(main())

#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import re
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CSS = ROOT / 'docs/templates/publisherstudio/public/main.css'
HELP = ROOT / 'src/PublisherStudio.Web/wwwroot/help-docs'
SNAPSHOT = ROOT / '.github/pages/publisherstudio-kawaii-docs.zip'


def fail(message: str) -> None:
    raise SystemExit(f'PublisherStudio 3.7.4 audit failed: {message}')


def main() -> int:
    web_project = (ROOT / 'src/PublisherStudio.Web/PublisherStudio.Web.csproj').read_text(encoding='utf-8')
    installer_project = (ROOT / 'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj').read_text(encoding='utf-8')
    if '<Version>3.7.4</Version>' not in web_project or '<Version>3.7.4</Version>' not in installer_project:
        fail('project versions are not synchronized at 3.7.4')

    release_markers = {
        'RELEASE.md': '# PublisherStudio 3.7.4',
        'VALIDATION.md': '# PublisherStudio 3.7.4 source validation',
        'CHANGELOG-v3.7.4-API-NAVIGATION-PADDING-REPAIR.md': '# PublisherStudio 3.7.4',
        'VALIDATION-v3.7.4-source.md': '# PublisherStudio 3.7.4 source validation',
        'docs/index.md': '**Version 3.7.4**',
        'docs/docfx.json': '"publisherstudioVersion": "3.7.4"',
        'docs/pdf/toc.yml': 'PublisherStudio-3.7.4.pdf',
        'src/PublisherStudio.Web/Components/App.razor': 'site.css?v=3.7.4',
    }
    for relative, marker in release_markers.items():
        text = (ROOT / relative).read_text(encoding='utf-8')
        if marker not in text:
            fail(f'{relative}: missing {marker!r}')

    css = CSS.read_text(encoding='utf-8')
    selector = 'html.publisherstudio-kawaii-docs body[data-yaml-mime="ManagedReference"] > main > .toc-offcanvas .offcanvas-body {'
    if selector not in css or 'padding: .8rem .65rem;' not in css[css.index(selector):css.index(selector) + 220]:
        fail('ManagedReference left navigation does not inherit the Overview/Guide padding')

    css_bytes = CSS.read_bytes()
    for relative in (
        'src/PublisherStudio.Web/wwwroot/help-docs/public/main.css',
        'src/PublisherStudio.Web/wwwroot/help-docs/styles/publisherstudio-kawaii.css',
    ):
        if (ROOT / relative).read_bytes() != css_bytes:
            fail(f'{relative} differs from the maintained documentation CSS')

    api_page = (HELP / 'api/PublisherStudio.Controllers.html').read_text(encoding='utf-8', errors='ignore')
    if 'data-yaml-mime="ManagedReference"' not in api_page or '<div class="toc-offcanvas">' not in api_page:
        fail('shipped API page no longer matches the selector contract used by the padding repair')

    css_hash = hashlib.sha256(css_bytes).hexdigest()[:12]
    stale = []
    pattern = re.compile(r'publisherstudio-kawaii\.css\?v=([0-9a-fA-F]+)')
    for html in HELP.rglob('*.html'):
        text = html.read_text(encoding='utf-8', errors='ignore')
        match = pattern.search(text)
        if match and match.group(1).lower() != css_hash:
            stale.append(str(html.relative_to(ROOT)))
    if stale:
        fail(f'stale documentation CSS cache key remains in {stale[0]}')

    with zipfile.ZipFile(SNAPSHOT) as archive:
        if archive.testzip() is not None:
            fail('tracked Pages snapshot is corrupt')
        for name in ('public/main.css', 'styles/publisherstudio-kawaii.css'):
            if archive.read(name) != css_bytes:
                fail(f'tracked Pages snapshot {name} is not synchronized')
        if b'PublisherStudio-3.7.4.pdf' not in archive.read('index.html'):
            fail('tracked Pages snapshot index was not synchronized to 3.7.4')

    print('PublisherStudio 3.7.4 audit passed: API navigation padding, version metadata, shipped CSS copies, cache keys and Pages snapshot are synchronized.')
    return 0


if __name__ == '__main__':
    sys.exit(main())

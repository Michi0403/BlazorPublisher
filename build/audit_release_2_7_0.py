#!/usr/bin/env python3
"""Source-only regression audit for PublisherStudio 2.7.0 shared Local Chat localization cleanup."""
from pathlib import Path
import json
import re
import sys

root = Path(__file__).resolve().parents[1]
web = root / "src/PublisherStudio.Web"


def read(rel):
    path = root / rel
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding="utf-8")


def require(rel, *needles):
    text = read(rel)
    missing = [needle for needle in needles if needle not in text]
    if missing:
        raise AssertionError(f"{rel} missing {missing}")


try:
    for rel in [
        "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
        "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
    ]:
        require(rel, "<Version>2.7.5</Version>")
        match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", read(rel))
        if not match or int(match.group(2)) > 9 or int(match.group(3)) > 9:
            raise AssertionError(f"version-slot policy failed for {rel}")

    for rel in [
        "src/PublisherStudio.Web/Components/App.razor",
        "src/PublisherStudio.Web/Components/Pages/Editor.razor",
        "src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor",
        "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
    ]:
        require(rel, "v=2.7.5")

    cultures = ["de-DE", "en-US", "es-ES", "fr-FR", "ja-JP", "uk-UA"]
    catalogs = {}
    obsolete = {
        "Phrase.Local␠ChatGPT",
        "Phrase.Welcome␠to␠your␠LocalChatGPT",
        "Text.Local␠ChatGPT",
        "Text.Welcome␠to␠your␠LocalChatGPT",
        "Text.Welcome␠to␠your␠LocalChatGPT,␠go␠to␠the␠Setup␠Page␠to␠Setup␠the␠AI␠Chat␠Clients",
    }
    required = {
        "Phrase.Local␠Chat",
        "Text.Local␠Chat",
        "Phrase.Welcome␠to␠your␠Local␠Chat",
        "Text.Welcome␠to␠your␠Local␠Chat",
        "Text.Welcome␠to␠your␠Local␠Chat,␠go␠to␠the␠Setup␠Page␠to␠Setup␠the␠AI␠Chat␠Clients",
    }
    for culture in cultures:
        rel = f"src/PublisherStudio.Web/Localization/{culture}.json"
        catalog = json.loads(read(rel))
        catalogs[culture] = catalog
        missing = sorted(required - catalog.keys())
        if missing:
            raise AssertionError(f"{culture} missing shared Local Chat localization keys: {missing}")
        present_obsolete = sorted(obsolete & catalog.keys())
        if present_obsolete:
            raise AssertionError(f"{culture} still contains obsolete LocalChatGPT localization keys: {present_obsolete}")
        for key in required:
            value = str(catalog[key])
            if not value.strip() or "ChatGPT" in value:
                raise AssertionError(f"{culture} has invalid Local Chat localization value at {key}: {value}")

    baseline_keys = set(catalogs["en-US"])
    for culture in cultures[1:]:
        if set(catalogs[culture]) != baseline_keys:
            raise AssertionError(f"localization key parity differs for {culture}")

    modes = []
    for path in web.rglob("*.razor"):
        for line in path.read_text(encoding="utf-8").splitlines():
            if "@rendermode" in line:
                modes.append((str(path.relative_to(root)), line.strip()))
    if len(modes) != 5:
        raise AssertionError(f"expected 5 PublisherStudio rendermode directives, found {len(modes)}")

    print("PublisherStudio 2.7.0 shared Local Chat localization source audit passed.")
except (AssertionError, OSError, json.JSONDecodeError) as exc:
    print(f"PublisherStudio 2.7.0 source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)

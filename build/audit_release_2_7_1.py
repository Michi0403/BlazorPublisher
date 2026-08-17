#!/usr/bin/env python3
"""Source-only paired release audit for PublisherStudio 2.7.1."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]


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
        require(rel, "<Version>2.7.8</Version>")
        match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", read(rel))
        if not match or int(match.group(2)) > 9 or int(match.group(3)) > 9:
            raise AssertionError(f"version-slot policy failed for {rel}")

    for rel in [
        "src/PublisherStudio.Web/Components/App.razor",
        "src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor",
        "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
        "src/PublisherStudio.Web/Components/Pages/Editor.razor",
    ]:
        require(rel, "v=2.7.8")

    modes = []
    for path in (root / "src/PublisherStudio.Web").rglob("*.razor"):
        for line in path.read_text(encoding="utf-8").splitlines():
            if "@rendermode" in line:
                modes.append((str(path.relative_to(root)), line.strip()))
    if len(modes) != 5:
        raise AssertionError(f"expected 5 PublisherStudio rendermode directives, found {len(modes)}")

    print("PublisherStudio 2.7.1 paired release source audit passed.")
except (AssertionError, OSError) as exc:
    print(f"PublisherStudio 2.7.1 source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)

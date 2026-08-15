#!/usr/bin/env python3
"""Source-only regression audit for PublisherStudio 2.6.9 LocalGPT session durability."""
from pathlib import Path
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
        require(rel, "<Version>2.7.6</Version>")
        match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", read(rel))
        if not match or int(match.group(2)) > 9 or int(match.group(3)) > 9:
            raise AssertionError(f"version-slot policy failed for {rel}")

    require("Directory.Build.props", "<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>")
    require(
        "src/PublisherStudio.Web/Services/OrganicPlugins/PublisherAiBridgeService.cs",
        "SaveToMemory = true,",
        "Publisher-started Council work is intentionally a normal LocalGPT /chat session",
    )
    require("src/PublisherStudio.Web/Components/App.razor", "v=2.7.6")

    modes=[]
    for path in web.rglob("*.razor"):
        for line in path.read_text(encoding="utf-8").splitlines():
            if "@rendermode" in line:
                modes.append((str(path.relative_to(root)), line.strip()))
    if len(modes) != 5:
        raise AssertionError(f"expected 5 PublisherStudio rendermode directives, found {len(modes)}")

    print("PublisherStudio 2.6.9 LocalGPT session durability source audit passed.")
except (AssertionError, OSError) as exc:
    print(f"PublisherStudio 2.6.9 source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)

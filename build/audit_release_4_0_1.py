from __future__ import annotations

from pathlib import Path
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
failures: list[str] = []


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        failures.append(f"missing {rel}")
        return ""
    return path.read_text(encoding="utf-8")


def require(rel: str, token: str, label: str) -> None:
    if token not in read(rel):
        failures.append(f"{label} missing from {rel}: {token}")


def reject(rel: str, token: str, label: str) -> None:
    if token in read(rel):
        failures.append(f"{label} still present in {rel}: {token}")


story_rel = "src/PublisherStudio.Web/Components/Editor/StoryEditor.razor"
require(story_rel, 'DocumentContent="@_content"', "one-way RichEdit document initialization")
require(story_rel, 'SelectionChanged="HandleSelectionChanged"', "observed-only RichEdit selection")
reject(story_rel, '@bind-DocumentContent="_content"', "server-controlled RichEdit live document")
reject(story_rel, '@bind-Selection="_selection"', "server-controlled RichEdit live selection")
require(story_rel, "_suppressSelectionRender = true;", "RichEdit selection notification render suppression")

media_rel = "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor"
require(media_rel, 'ValueChangeMode="RangeSelectorValueChangeMode.OnHandleRelease"', "Media Studio range commit boundary")
reject(media_rel, 'ValueChangeMode="RangeSelectorValueChangeMode.OnHandleMove"', "Media Studio server-round-tripped handle movement")
require("src/PublisherStudio.Web/Components/Editor/PublicationTimeline.razor", 'ValueChangeMode="RangeSelectorValueChangeMode.OnHandleRelease"', "Timeline range commit boundary")

for rel, token, label in (
    ("build/audit_transient_ui_state_ownership.py", "DxRichEdit must not two-way bind transient", "generic RichEdit ownership guard"),
    ("build/audit_transient_ui_state_ownership.py", "DxRangeSelector may not round-trip every handle move", "generic range ownership guard"),
    ("build/Assert-TransientUiStateOwnership.ps1", "Transient UI-state ownership validation failed", "PowerShell transient-state wrapper"),
    ("Directory.Build.targets", "TransientUiStateOwnershipScript", "build property for transient-state guard"),
    ("Directory.Build.targets", "AssertPublisherTransientUiStateOwnership", "build target for transient-state guard"),
    ("build/Assert-DevExpressComponentRetention.ps1", "TransientUiStateOwnershipScript", "retention guard wiring check"),
    ("AGENTS.md", "## InteractiveServer transient UI-state ownership", "repository transient-state rule"),
    ("src/PublisherStudio.Web/wwwroot/js/publisherInterop.js", "function installStableNativeRangeLifecycle()", "reviewed native-range coalescer"),
    ("src/PublisherStudio.Web/wwwroot/js/publisherInterop.js", "target.closest('.dxreRoot, .dx-widget, [class*=\"dxbl-\"], [data-dxbl-loaded]')", "vendor exclusion from native-range coalescer"),
):
    require(rel, token, label)

result = subprocess.run(
    [sys.executable, str(ROOT / "build/audit_transient_ui_state_ownership.py"), "--root", str(ROOT), "--product", "publisherstudio"],
    text=True,
    capture_output=True,
)
if result.returncode != 0:
    failures.append("generic transient UI-state ownership audit failed: " + (result.stderr or result.stdout).strip())

require("CHANGELOG-v4.0.1-TRANSIENT-UI-STATE-OWNERSHIP.md", "# PublisherStudio 4.0.1", "4.0.1 changelog")
require("RELEASE.md", "# PublisherStudio 4.0.1", "active release notes")
require("VALIDATION-v4.0.1-source.md", "# PublisherStudio 4.0.1 source validation", "versioned source validation")
require("VALIDATION.md", "# PublisherStudio 4.0.1 source validation", "active source validation")

version = "4.0.1"
version_files = (
    "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
    "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
    "src/PublisherStudio.Web/package.json",
    "src/PublisherStudio.Web/package-lock.json",
    "src/PublisherStudio.Web/Components/App.razor",
    "src/PublisherStudio.Web/Components/Pages/Editor.razor",
    "src/PublisherStudio.Web/Components/Editor/PageSurface.razor",
    "src/PublisherStudio.Web/Components/Editor/BarcodeEditor.razor",
    "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
    "src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor",
    "src/PublisherStudio.Web/Components/Editor/WordArtPathEditor.razor",
)
for rel in version_files:
    if version not in read(rel):
        failures.append(f"{version} active identity missing from {rel}")

m = re.fullmatch(r"(\d+)\.(\d+)\.(\d+)", version)
if not m or int(m.group(2)) > 9 or int(m.group(3)) > 9:
    failures.append(f"release version violates single-digit minor/patch policy: {version}")

for rel in (
    "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
    "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
):
    try:
        root = ET.fromstring((ROOT / rel).read_text(encoding="utf-8"))
        found = root.findtext(".//Version")
        if found != version:
            failures.append(f"{rel} Version is {found!r}, expected {version!r}")
    except Exception as exc:
        failures.append(f"could not parse {rel}: {exc}")

for rel in ("src/PublisherStudio.Web/package.json", "src/PublisherStudio.Web/package-lock.json"):
    try:
        data = json.loads((ROOT / rel).read_text(encoding="utf-8"))
        if data.get("version") != version:
            failures.append(f"{rel} root version is {data.get('version')!r}, expected {version!r}")
        if rel.endswith("package-lock.json") and data.get("packages", {}).get("", {}).get("version") != version:
            failures.append("package-lock root package version is not 4.0.1")
    except Exception as exc:
        failures.append(f"could not parse {rel}: {exc}")

try:
    ET.parse(ROOT / "Directory.Build.targets")
except Exception as exc:
    failures.append(f"Directory.Build.targets XML is invalid: {exc}")

# No browser JavaScript changed in this release; prove the carried diagnostics manifest still matches.
manifest = read("build/javascript-diagnostics-files.sha256")
for line in manifest.splitlines():
    match = re.fullmatch(r"([0-9a-f]{64})  (.+\.js)", line)
    if not match:
        continue
    rel = match.group(2)
    path = ROOT / rel
    if not path.is_file():
        failures.append(f"JavaScript diagnostics manifest file missing: {rel}")
        continue
    normalized = path.read_bytes().replace(b"\r\n", b"\n").replace(b"\r", b"\n")
    actual = hashlib.sha256(normalized).hexdigest()
    if actual != match.group(1):
        failures.append(f"JavaScript diagnostics hash mismatch: {rel}")

if failures:
    for failure in failures:
        print(f"FAIL: {failure}", file=sys.stderr)
    raise SystemExit(1)

print(
    "PublisherStudio 4.0.1 transient UI-state ownership audit passed: Story RichEdit ownership remains fixed, Media Studio range drag commits on release, "
    "the generic build-breaking ownership guard is wired/protected, native live-range coalescing remains vendor-safe, and active release identity is aligned."
)

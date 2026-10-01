from __future__ import annotations
from pathlib import Path
import hashlib
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
failures: list[str] = []

def read(rel: str) -> str:
    p = ROOT / rel
    if not p.is_file():
        failures.append(f"missing {rel}")
        return ""
    return p.read_text(encoding="utf-8")

def require(rel: str, token: str, label: str) -> None:
    if token not in read(rel):
        failures.append(f"{label} missing from {rel}: {token}")

def reject(rel: str, token: str, label: str) -> None:
    if token in read(rel):
        failures.append(f"{label} still present in {rel}: {token}")

localization = read("src/PublisherStudio.Web/wwwroot/js/localizationRuntime.js")
if not re.search(r"excludedSelector\s*=\s*'[^']*\.dxreRoot[^']*\.story-rich-edit[^']*'", localization):
    failures.append("RichEdit roots are not protected from application DOM localization")

interop = read("src/PublisherStudio.Web/wwwroot/js/publisherInterop.js")
require("src/PublisherStudio.Web/wwwroot/js/publisherInterop.js", "'.dxreRoot'", "RichEdit shared input ownership")
require("src/PublisherStudio.Web/wwwroot/js/publisherInterop.js", "if (currentHost?.contains(event.target)) return;", "RichEdit shell-click isolation")
story_start = interop.find("function initializeStoryEditorLayout(")
story_end = interop.find("let dataVisualLayoutTimer", story_start)
if story_start < 0 or story_end < 0:
    failures.append("Story Editor layout block could not be located")
else:
    story_block = interop[story_start:story_end]
    if "window.dispatchEvent(new Event('resize'))" in story_block:
        failures.append("Story Editor layout bridge must not synthesize global resize events")

inspector = read("src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor")
if '<DxButton Text=\'@LT("Edit all application strings")\'' not in inspector:
    failures.append("translation action is not a DevExpress DxButton")
if '<button type="button" @onclick="OpenTranslationEditor">' in inspector:
    failures.append("native translation button remains")

css = read("src/PublisherStudio.Web/wwwroot/css/site.css")
for token in (
    ".property-button-row:has(> :only-child)",
    ".localization-action-button .dxbl-btn-caption",
    "white-space: normal",
):
    if token not in css:
        failures.append(f"inspector button containment CSS missing: {token}")

for rel in (
    "build/Assert-LocalizationIntegrity.ps1",
    "build/Assert-PanelStudioInteractionLifecycle.ps1",
):
    require(rel, ".dxreRoot", "build-wired RichEdit ownership guard")

version_files = (
    "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
    "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
    "src/PublisherStudio.Web/package.json",
    "src/PublisherStudio.Web/package-lock.json",
    "src/PublisherStudio.Web/Components/App.razor",
)
for rel in version_files:
    if "3.9.9" not in read(rel):
        failures.append(f"3.9.9 identity missing from {rel}")

manifest = read("build/javascript-diagnostics-files.sha256")
for line in manifest.splitlines():
    m = re.fullmatch(r"([0-9a-f]{64})  (.+\.js)", line)
    if not m:
        continue
    rel = m.group(2)
    p = ROOT / rel
    if not p.is_file():
        failures.append(f"JavaScript diagnostics manifest file missing: {rel}")
        continue
    normalized = p.read_bytes().replace(b"\r\n", b"\n").replace(b"\r", b"\n")
    actual = hashlib.sha256(normalized).hexdigest()
    if actual != m.group(1):
        failures.append(f"JavaScript diagnostics hash mismatch: {rel}")

if failures:
    for failure in failures:
        print(f"FAIL: {failure}", file=sys.stderr)
    raise SystemExit(1)

print("PublisherStudio 3.9.9 Story RichEdit ownership source audit passed: document DOM localization isolation, RichEdit input/layout ownership, inspector DxButton containment, release identity, and JavaScript diagnostics hashes are consistent.")

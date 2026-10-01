from __future__ import annotations

from pathlib import Path
import hashlib
import json
import re
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
story = read(story_rel)
require(story_rel, 'DocumentContent="@_content"', "one-way RichEdit document initialization")
require(story_rel, 'SelectionChanged="HandleSelectionChanged"', "observed-only RichEdit selection")
reject(story_rel, '@bind-DocumentContent="_content"', "server-controlled live RichEdit document binding")
reject(story_rel, '@bind-Selection="_selection"', "server-controlled live RichEdit selection binding")
require(story_rel, "private void HandleSelectionChanged(Selection selection)", "selection observer")
require(story_rel, "_suppressSelectionRender = true;", "selection-event render suppression")
if not re.search(r"protected override bool ShouldRender\(\)[\s\S]{0,520}if \(_suppressSelectionRender\)[\s\S]{0,220}return false;", story):
    failures.append("StoryEditor does not suppress the automatic render caused by RichEdit selection notifications")
if story.count("_selection = default!;") < 3:
    failures.append("RichEdit selection is not reset for load, close, and document-generation replacement")
if not re.search(
    r"_mailMergeSettingsContent = null;\s*_selection = default!;\s*_editorRevision\+\+;",
    story,
):
    failures.append("legacy RichEdit document replacement does not discard the previous-generation selection")
if not re.search(
    r"if \(!Visible\)[\s\S]{0,420}_selection = default!;\s*_richEdit = null;",
    story,
):
    failures.append("Story Editor close does not release stale selection/RichEdit instance state")

# Keep the previous ownership protections that removed the independent 3.9.9 DOM/localization race.
localization = read("src/PublisherStudio.Web/wwwroot/js/localizationRuntime.js")
if not re.search(r"excludedSelector\s*=\s*'[^']*\.dxreRoot[^']*\.story-rich-edit[^']*'", localization):
    failures.append("RichEdit roots are not protected from application DOM localization")
interop = read("src/PublisherStudio.Web/wwwroot/js/publisherInterop.js")
for token, label in (
    ("'.dxreRoot'", "RichEdit shared input ownership"),
    ("if (currentHost?.contains(event.target)) return;", "RichEdit shell-click isolation"),
):
    if token not in interop:
        failures.append(f"{label} missing from publisherInterop.js: {token}")

# The build-wired maintenance guard must prevent the controlled-editor architecture from returning.
guard_rel = "build/Assert-PanelStudioInteractionLifecycle.ps1"
for token, label in (
    ('DocumentContent="@_content"', "build guard for one-way RichEdit document state"),
    ('SelectionChanged="HandleSelectionChanged"', "build guard for observed-only RichEdit selection"),
    ('@bind-DocumentContent="_content"', "build rejection for RichEdit document two-way binding"),
    ('@bind-Selection="_selection"', "build rejection for RichEdit selection two-way binding"),
    ('_suppressSelectionRender\\s*=\\s*true;', "build guard for non-rendering RichEdit selection observation"),
):
    if token not in read(guard_rel):
        failures.append(f"{label} missing from {guard_rel}")

# Release documentation is part of the source contract.
require("CHANGELOG-v4.0.0-STORY-RICHEDIT-LIVE-STATE-OWNERSHIP.md", "# PublisherStudio 4.0.0", "4.0.0 changelog")
require("CHANGELOG-v4.0.0-STORY-RICHEDIT-LIVE-STATE-OWNERSHIP.md", "@bind-DocumentContent", "document-binding regression explanation")
require("RELEASE.md", "# PublisherStudio 4.0.0", "active release notes")
require("VALIDATION-v4.0.0-source.md", "# PublisherStudio 4.0.0 source validation", "versioned source validation")
require("VALIDATION.md", "# PublisherStudio 4.0.0 source validation", "active source validation")

# Active release identity. Historical changelogs/validations intentionally retain their release versions.
version = "4.0.0"
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

# Parse active package/project metadata rather than relying only on textual identity checks.
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
            failures.append("package-lock root package version is not 4.0.0")
    except Exception as exc:
        failures.append(f"could not parse {rel}: {exc}")

# No browser JavaScript changed in this release; still prove the carried diagnostics manifest is intact.
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
    "PublisherStudio 4.0.0 Story RichEdit live-state ownership source audit passed: "
    "live document/selection are not two-way rebound, selection notifications do not rerender "
    "StoryEditor, selection generations are reset, prior RichEdit DOM/input protections remain, active "
    "release identities are aligned, and JavaScript diagnostics hashes are intact."
)

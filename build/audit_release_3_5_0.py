#!/usr/bin/env python3
"""Source-only release checks for PublisherStudio 3.5.0."""
from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION = "3.5.0"
ERRORS: list[str] = []


def fail(message: str) -> None:
    ERRORS.append(message)


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        fail(f"missing file: {rel}")
        return ""
    return path.read_text(encoding="utf-8-sig", errors="replace")


def require(rel: str, needle: str) -> None:
    if needle not in read(rel):
        fail(f"{rel}: missing {needle!r}")


for rel in (
    "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
    "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
):
    text = read(rel)
    try:
        version = ET.fromstring(text).findtext("PropertyGroup/Version")
    except ET.ParseError as exc:
        fail(f"{rel}: invalid XML: {exc}")
        continue
    if version != VERSION:
        fail(f"{rel}: expected Version {VERSION}, found {version!r}")
    if version and not re.fullmatch(r"\d+\.\d\.\d", version):
        fail(f"{rel}: version violates one-digit minor/patch policy: {version}")

for rel in ("src/PublisherStudio.Web/package.json", "src/PublisherStudio.Web/package-lock.json"):
    try:
        data = json.loads(read(rel))
    except json.JSONDecodeError as exc:
        fail(f"{rel}: invalid JSON: {exc}")
        continue
    if data.get("version") != VERSION:
        fail(f"{rel}: expected top-level version {VERSION}, found {data.get('version')!r}")
    if rel.endswith("package-lock.json") and data.get("packages", {}).get("", {}).get("version") != VERSION:
        fail(f"{rel}: expected root package version {VERSION}")

for rel, needle in (
    ("docs/index.md", f"Version {VERSION}"),
    ("docs/docfx.json", f'"publisherstudioVersion": "{VERSION}"'),
    ("docs/pdf-cover.html", f"Version {VERSION}"),
    ("docs/pdf/toc.yml", f"PublisherStudio-{VERSION}.pdf"),
    ("RELEASE.md", f"# PublisherStudio {VERSION}"),
    ("CHANGELOG-v3.5.0-POWERSHELL51-DOCUMENTATION-CACHE-REPAIR.md", "PowerShell 5.1 documentation-cache and Debug PDF contract repair"),
    ("VALIDATION-v3.5.0-source.md", "# PublisherStudio 3.5.0 source validation"),
):
    require(rel, needle)

# Exact Windows PowerShell 5.1 documentation-cache regression repair, ported from LocalGPT.
docs = read("build/Build-Documentation.ps1")
fixed_cache_join = "Join-Path (Join-Path $temporary 'site') $entry.Name"
legacy_cache_join = "Join-Path $temporary 'site' $entry.Name"
if fixed_cache_join not in docs:
    fail("documentation cache does not use nested two-argument Join-Path calls")
if legacy_cache_join in docs:
    fail("documentation cache still uses PowerShell 6+ three-positional-argument Join-Path")

compat = read("build/Assert-PowerShellCompatibility.ps1")
for marker in (
    "$scriptAst = [System.Management.Automation.Language.Parser]::ParseInput",
    "AdditionalChildPath",
    "$joinPathCommands",
    "$commandAst.Extent.StartLineNumber",
    "passes more than Path + ChildPath positionally to Join-Path",
):
    if marker not in compat:
        fail(f"PowerShell 5.1 Join-Path guard missing: {marker}")

# Port the proven LocalGPT Debug documentation contract follow-up as well.
targets = read("Directory.Build.targets")
for marker in (
    "RequirePublisherStudioDocumentationPdf Condition=\"'$(RequirePublisherStudioDocumentationPdf)' == '' and '$(Configuration)' == 'Release'\">true",
    "PublisherStudioDocumentationPdfArgument Condition=\"'$(RequirePublisherStudioDocumentationPdf)' == 'true'\">-RequirePdf",
):
    if marker not in targets:
        fail(f"Directory.Build.targets Debug/Release PDF contract marker missing: {marker}")
for marker in (
    "if ($RequirePdf -or $pdfGenerated) {",
    "HTML-only Debug documentation intentionally has no standalone PDF",
    "Complete PDF generation was explicitly disabled; this HTML-only diagnostic build does not emit a fallback PDF.",
):
    if marker not in docs:
        fail(f"Build-Documentation Debug PDF contract marker missing: {marker}")
old_final_pdf_gate = "if (Test-Path -LiteralPath $sourceWebRoot -PathType Container) {\n    $sourcePdfPath = Join-Path $sourceWebRoot $pdfName"
if old_final_pdf_gate in docs:
    fail("Build-Documentation still performs the old unconditional Debug embedded-PDF source-tree assertion")

# Preserve the 3.4.9 UI/localization repair while changing only the documentation build path.
page_rel = "src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor"
page = read(page_rel)
for needle in ("Back to PublisherStudio", "PublisherStudio organic capabilities"):
    if needle not in page:
        fail(f"{page_rel}: missing corrected UI wording {needle!r}")
if ">Back to Publisher</button>" in page or "<h2>PublisherStudio organ capabilities</h2>" in page:
    fail(f"{page_rel}: obsolete UI naming remains")

localization_dir = ROOT / "src/PublisherStudio.Web/Localization"
catalog_paths = sorted(localization_dir.glob("*.json"))
if not catalog_paths:
    fail("no PublisherStudio localization catalogs found")
else:
    catalogs: dict[str, dict[str, str]] = {}
    for path in catalog_paths:
        try:
            catalogs[path.name] = json.loads(path.read_text(encoding="utf-8-sig"))
        except (OSError, json.JSONDecodeError) as exc:
            fail(f"{path.relative_to(ROOT)}: invalid JSON: {exc}")
    if catalogs:
        reference_name, reference = next(iter(catalogs.items()))
        keys = set(reference)
        corrected = "Text.PublisherStudio␠organic␠capabilities"
        legacy = "Text.PublisherStudio␠organ␠capabilities"
        for name, catalog in catalogs.items():
            if set(catalog) != keys:
                fail(f"localization key parity mismatch {name} vs {reference_name}")
            if corrected not in catalog:
                fail(f"{name}: corrected organic-capabilities key missing")
            if legacy not in catalog:
                fail(f"{name}: compatibility key for historical typo was removed")

# Routed pages retain existing InteractiveServer ownership; Error stays intentionally static.
pages = ROOT / "src/PublisherStudio.Web/Components/Pages"
route_count = 0
interactive_count = 0
for path in sorted(pages.rglob("*.razor")):
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    if not re.search(r'^\s*@page\s+"', text, flags=re.MULTILINE):
        continue
    route_count += 1
    is_error = path.name == "Error.razor"
    has_interactive = bool(re.search(r"^\s*@rendermode\s+InteractiveServer\s*$", text, flags=re.MULTILINE))
    if has_interactive:
        interactive_count += 1
    if not is_error and not has_interactive:
        fail(f"{path.relative_to(ROOT)}: routed page is missing @rendermode InteractiveServer")
    if is_error and has_interactive:
        fail(f"{path.relative_to(ROOT)}: Error page should remain intentionally static")
if route_count < 2:
    fail(f"unexpected routed page count: {route_count}")

for path in ROOT.rglob("*"):
    if path.is_dir() and path.name in {"bin", "obj"}:
        fail(f"compiled-artifact directory present: {path.relative_to(ROOT)}")
        break

if ERRORS:
    print("PublisherStudio 3.5.0 source audit FAILED:")
    for error in ERRORS:
        print(f" - {error}")
    sys.exit(1)
print(
    f"PublisherStudio {VERSION} source audit passed: "
    f"{route_count} routed pages, {interactive_count} InteractiveServer routed boundaries, "
    f"{len(catalog_paths)} localization catalogs, PowerShell 5.1 Join-Path cache repair guarded."
)

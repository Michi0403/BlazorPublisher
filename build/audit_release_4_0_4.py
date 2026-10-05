from __future__ import annotations

from pathlib import Path
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
failures: list[str] = []


def fail(message: str) -> None:
    failures.append(message)


def text(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        fail(f"missing {rel}")
        return ""
    return path.read_text(encoding="utf-8-sig")


def markup(rel: str) -> str:
    return text(rel).split("@code", 1)[0]


def run(args: list[str]) -> None:
    result = subprocess.run(args, cwd=ROOT)
    if result.returncode:
        fail("command failed: " + " ".join(args))


# Release identity.
for rel in (
    "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
    "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
):
    path = ROOT / rel
    if not path.is_file():
        fail(f"missing {rel}")
        continue
    if (ET.parse(path).findtext(".//Version") or "").strip() != "4.0.4":
        fail(f"{rel} version mismatch")

for rel in ("src/PublisherStudio.Web/package.json", "src/PublisherStudio.Web/package-lock.json"):
    data = json.loads(text(rel) or "{}")
    if data.get("version") != "4.0.4":
        fail(f"{rel} version mismatch")
    if rel.endswith("package-lock.json") and data.get("packages", {}).get("", {}).get("version") != "4.0.4":
        fail(f"{rel} root package version mismatch")

for rel in (
    "CHANGELOG-v4.0.4-DEVEXPRESS-UI-PANEL-RENDERER-REPAIR.md",
    "VALIDATION-v4.0.4-source.md",
    "RELEASE.md",
):
    if not (ROOT / rel).is_file():
        fail(f"missing {rel}")

# System-font picker must stay DevExpress-native and searchable.
font_picker = markup("src/PublisherStudio.Web/Components/Editor/SystemFontPicker.razor")
for token in ("<DxComboBox", 'AllowUserInput="true"', "ListSearchMode.AutoSearch", "ListRenderMode.Virtual"):
    if token not in font_picker:
        fail(f"SystemFontPicker missing maintained DevExpress token: {token}")
if re.search(r"<(?:input|select|datalist)\b", font_picker, re.IGNORECASE):
    fail("SystemFontPicker regressed to native input/select/datalist; use the searchable DevExpress picker")

# High-value native substitutions are forbidden across maintained Razor UI.
razor_root = ROOT / "src/PublisherStudio.Web"
for path in razor_root.rglob("*.razor"):
    source = path.read_text(encoding="utf-8-sig").split("@code", 1)[0]
    rel = path.relative_to(ROOT).as_posix()
    if re.search(r"<datalist\b", source, re.IGNORECASE):
        fail(f"{rel} contains native <datalist>; use a searchable DevExpress editor")
    if re.search(r"<input\b[^>]*\btype\s*=\s*['\"]color['\"]", source, re.IGNORECASE | re.DOTALL):
        fail(f"{rel} contains native input type=color; use DxColorPalette integration")

# The reported Page Appearance & Effects surface must stay DevExpress-owned.
page_effect = markup("src/PublisherStudio.Web/Components/Editor/PageEffectStudio.razor")
for token in ("<DxButton", "<DxTextBox", "<DxCheckBox", "<DxComboBox", "<DxSpinEdit", "<DevExpressColorPicker"):
    if token not in page_effect:
        fail(f"PageEffectStudio missing DevExpress editor/action family: {token}")
if re.search(r"<(?:button|input|select|textarea|datalist|details|summary)\b", page_effect, re.IGNORECASE):
    fail("PageEffectStudio contains raw visible interactive HTML; keep this reported workflow DevExpress-owned")

# Panel/render geometry must use direct authored roots, not maintenance FormLayout wrappers.
structural = (
    "PanelView.razor",
    "DataVisualView.razor",
    "BarcodeView.razor",
    "PageEffectLayerRenderer.razor",
    "DataVisualClientHost.razor",
    "DevExtremeComponentView.razor",
    "PanelElementPreview.razor",
    "LiveSourceView.razor",
    "VideoMediaView.razor",
    "HtmlEmbedView.razor",
)
for name in structural:
    rel = f"src/PublisherStudio.Web/Components/Editor/{name}"
    source = markup(rel)
    if "razor-structural-renderer" not in source:
        fail(f"{rel} is missing its structural-renderer reason marker")
    if "razor-component-layout-owner" in source or "razor-section-layout-owner" in source:
        fail(f"{rel} regained a maintenance semantic-layout wrapper that can alter authored geometry")

panel_view = markup("src/PublisherStudio.Web/Components/Editor/PanelView.razor")
if not re.search(r"razor-structural-renderer[\s\S]{0,700}<div class=\"publication-panel ", panel_view):
    fail("PanelView must expose the authored publication-panel root directly after its structural-renderer marker")
if "<div @key=\"view.Id\" class=\"publication-panel-view" not in panel_view:
    fail("PanelView must render publication-panel-view directly rather than through a maintenance FormLayout")

panel_studio = markup("src/PublisherStudio.Web/Components/Editor/PanelStudio.razor")
if panel_studio.count("<button") != 2:
    fail("PanelStudio native buttons changed; only reviewed browser-owned drag-source and hit-test primitives are allowed")
for reason in ("Browser-owned drag source", "Browser-owned authored hit-test surface"):
    if reason not in panel_studio:
        fail(f"PanelStudio native browser primitive is missing its maintenance reason: {reason}")

# Retention baseline must cover every Razor file.
manifest_path = ROOT / "build/devexpress-component-retention.json"
manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig")) if manifest_path.is_file() else {}
entries = manifest.get("Files", {})
razor_files = {p.relative_to(ROOT).as_posix() for p in razor_root.rglob("*.razor")}
if set(entries) != razor_files:
    missing = sorted(razor_files - set(entries))
    extra = sorted(set(entries) - razor_files)
    fail(f"DevExpress retention manifest coverage mismatch; missing={missing[:5]} extra={extra[:5]}")

# Run the maintained source-only gates most directly related to this repair.
run([sys.executable, str(ROOT / "build/audit_razor_maintenance_contract.py"), "--root", str(ROOT), "--product", "publisherstudio"])
run([sys.executable, str(ROOT / "build/audit_transient_ui_state_ownership.py"), "--root", str(ROOT), "--product", "publisherstudio"])
run([sys.executable, str(ROOT / "build/audit_panelstudio_persistence.py")])

if failures:
    print("\n".join("FAIL: " + x for x in failures), file=sys.stderr)
    raise SystemExit(1)
print("PublisherStudio 4.0.4 DevExpress UI / structural Panel renderer release audit passed.")

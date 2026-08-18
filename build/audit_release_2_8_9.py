#!/usr/bin/env python3
"""Static source audit for PublisherStudio 2.9.0 toolchain integration and component-diagnostics repair."""
from __future__ import annotations
from pathlib import Path
import json, re, sys

root = Path(__file__).resolve().parents[1]
checks = 0

def read(rel: str) -> str:
    p = root / rel
    if not p.is_file():
        raise AssertionError(f"missing {rel}")
    return p.read_text(encoding="utf-8-sig", errors="replace")

def require(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token not in data:
            raise AssertionError(f"{rel} missing {token!r}")

def forbid(rel: str, *tokens: str) -> None:
    global checks
    data = read(rel)
    for token in tokens:
        checks += 1
        if token in data:
            raise AssertionError(f"{rel} unexpectedly contains {token!r}")

try:
    for rel in (
        "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
        "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
    ):
        require(rel, "<Version>2.9.0</Version>")

    require("src/PublisherStudio.Web/PublisherStudio.Web.csproj", "<DevExpressVersion>25.2.9</DevExpressVersion>")
    require("src/PublisherStudio.Web/dotnet-tools.json", '"version": "10.0.11"')
    require("src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
            '<PackageReference Include="Microsoft.Extensions.Logging" Version="10.0.11" />')
    require("global.json", '"version": "10.0.301"', '"rollForward": "latestFeature"')

    require("src/PublisherStudio.Web/Components/App.razor",
            "css/site.css?v=20260818-290", "videoEffectRuntime.js?v=2.9.0", "publisherInterop.js?v=2.9.0")
    for rel in (
        "src/PublisherStudio.Web/Components/Pages/Editor.razor",
        "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
        "src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor",
    ):
        require(rel, "mediaStudioInterop.js?v=2.9.0")

    diagnostics = "build/Assert-ComponentDiagnostics.ps1"
    require(diagnostics,
            "Test-DocumentationOnlyRazorPartial",
            ".razor.cs",
            "XML-documentation companion files intentionally contain only an empty partial",
            "Any field, property, method",
            "$documentationOnlyPartials++")
    forbid(diagnostics, "component-diagnostics-doc-shell-baseline")
    for legacy in (
        "build/async-continuation-baseline.json",
        "build/component-method-resilience-baseline.json",
        "build/iterator-exception-baseline.json",
    ):
        checks += 1
        if (root / legacy).exists():
            raise AssertionError(f"legacy exemption baseline must stay removed: {legacy}")

    # Reproduce the strict whole-file classification in Python. Exactly the 46
    # documentation-only companions reported by the user's build must match; the
    # operational PictureEditor code-behind must not.
    components = root / "src/PublisherStudio.Web/Components"
    shell_pattern = re.compile(
        r"^\s*namespace\s+[A-Za-z_][A-Za-z0-9_.]*\s*;\s*"
        r"(?:(?:\s*///[^\r\n]*(?:\r?\n|$))+\s*)"
        r"public\s+partial\s+class\s+[A-Za-z_][A-Za-z0-9_]*\s*\{\s*\}\s*$",
        re.S,
    )
    shells: list[Path] = []
    operational: list[Path] = []
    for path in sorted(components.rglob("*.razor.cs")):
        sibling = Path(str(path)[:-3])
        data = path.read_text(encoding="utf-8-sig", errors="replace")
        if sibling.is_file() and shell_pattern.fullmatch(data):
            shells.append(path)
        else:
            operational.append(path)
    checks += 1
    if len(shells) != 46:
        raise AssertionError(f"expected 46 documentation-only Razor partials, found {len(shells)}")
    checks += 1
    operational_rel = [p.relative_to(components).as_posix() for p in operational]
    if operational_rel != ["Editor/PictureEditor.razor.cs"]:
        raise AssertionError(f"unexpected operational Razor code-behind set: {operational_rel}")

    baseline = json.loads(read("build/component-diagnostics-baseline.json"))
    checks += 1
    if "Components/Editor/PictureEditor.razor.cs" not in baseline.get("files", {}):
        raise AssertionError("operational PictureEditor.razor.cs left the diagnostics baseline")
    for shell in shells:
        rel = shell.relative_to(root / "src/PublisherStudio.Web").as_posix()
        checks += 1
        if rel in baseline.get("files", {}):
            raise AssertionError(f"documentation-only shell was added to operational baseline: {rel}")

    # Render and protocol boundaries remain unchanged by this build-policy repair.
    expected_modes = {
        "src/PublisherStudio.Web/Components/Pages/Editor.razor": "@rendermode InteractiveServer",
        "src/PublisherStudio.Web/Components/Pages/Help.razor": "@rendermode InteractiveServer",
        "src/PublisherStudio.Web/Components/Pages/Localization.razor": "@rendermode InteractiveServer",
        "src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor": "@rendermode InteractiveServer",
        "src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor": "@rendermode @(new InteractiveServerRenderMode(prerender: false))",
    }
    actual = []
    for path in components.rglob("*.razor"):
        data = path.read_text(encoding="utf-8-sig", errors="replace")
        if "@rendermode" in data:
            actual.append(path.relative_to(root).as_posix())
    checks += 1
    if set(actual) != set(expected_modes):
        raise AssertionError(f"render-mode set changed: {sorted(actual)}")
    for rel, directive in expected_modes.items():
        first = next(line.strip() for line in read(rel).splitlines() if line.strip())
        checks += 1
        if first != directive:
            raise AssertionError(f"{rel} first directive {first!r} != {directive!r}")
    require("Directory.Build.props", "<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>")

    require("CHANGELOG-v2.8.9-DOTNET-DEVEXPRESS-COMPONENT-DIAGNOSTICS-REPAIR.md",
            "DevExpress", "25.2.9", "10.0.11", "documentation-only")
    require("VALIDATION-v2.8.9-source.md", "No `dotnet`", "46 documentation companions")

    print(f"PublisherStudio 2.9.0 upgrade/component-diagnostics source audit passed: {checks} checks.")
except Exception as exc:
    print(f"PublisherStudio 2.9.0 source audit failed: {exc}", file=sys.stderr)
    raise SystemExit(1)

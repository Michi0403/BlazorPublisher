#!/usr/bin/env python3
"""Source-only release audit for PublisherStudio 3.0.4. Does not invoke dotnet/pwsh."""
from __future__ import annotations
from pathlib import Path
import json, re, subprocess, sys

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "src" / "PublisherStudio.Web"
FAIL: list[str] = []
PASS: list[str] = []

def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8-sig")

def require(rel: str, token: str, label: str) -> None:
    if token in read(rel): PASS.append(label)
    else: FAIL.append(f"{label}: missing {token}")

def forbid(rel: str, token: str, label: str) -> None:
    if token not in read(rel): PASS.append(label)
    else: FAIL.append(f"{label}: forbidden {token}")

identity_files = [
    "src/PublisherStudio.Web/PublisherStudio.Web.csproj",
    "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj",
    "src/PublisherStudio.Web/package.json",
    "src/PublisherStudio.Web/package-lock.json",
]
for rel in identity_files:
    require(rel, "3.0.4", f"3.0.4 identity {rel}")
package = json.loads(read("src/PublisherStudio.Web/package.json"))
major, minor, patch = map(int, package["version"].split("."))
if package["version"] == "3.0.4" and minor < 10 and patch < 10:
    PASS.append("single-digit release version policy")
else:
    FAIL.append(f"invalid release identity/version-slot policy: {package['version']}")
lock = json.loads(read("src/PublisherStudio.Web/package-lock.json"))
if lock.get("version") == "3.0.4" and lock.get("packages", {}).get("", {}).get("version") == "3.0.4": PASS.append("package-lock top identity")
else: FAIL.append("package-lock top identity is not 3.0.4")

for rel in ["RELEASE.md", "CHANGELOG-v3.0.4-NATIVE-DISCOVERY-COMPILE-PLATFORM-WARNING-REPAIR.md", "VALIDATION-v3.0.4-source.md"]:
    require(rel, "3.0.4", f"release documentation identity {rel}")

app = read("src/PublisherStudio.Web/Components/App.razor")
for asset in ["site.css", "localizationRuntime.js", "videoEffectRuntime.js", "componentRuntime.js", "publisherInterop.js"]:
    if f"{asset}?v=3.0.4" in app: PASS.append(f"cache identity {asset}")
    else: FAIL.append(f"cache identity missing for {asset}")
require("src/PublisherStudio.Web/Components/Editor/MediaStudio.razor", "mediaStudioInterop.js?v=3.0.4", "MediaStudio module identity")
require("src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor", "mediaStudioInterop.js?v=3.0.4", "Inspector module identity")

csproj = read("src/PublisherStudio.Web/PublisherStudio.Web.csproj")
for package_name in ["System.Drawing.Common", "System.Security.Cryptography.ProtectedData"]:
    if package_name not in csproj: PASS.append(f"unused Windows-only package removed: {package_name}")
    else: FAIL.append(f"Windows-only/unused package remains: {package_name}")
for token in ["PublisherStudioPowerShellHost", ">pwsh<", "System.IO.Path]::GetFullPath", "/../../Prepare-DevExpressAssets.ps1"]:
    if token in csproj: PASS.append(f"cross-platform project command marker {token}")
    else: FAIL.append(f"missing cross-platform project command marker {token}")

registration = read("src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs")
for marker in [
    "WindowsPublisherPlatformRuntimeService", "UnixPublisherPlatformRuntimeService",
    "WindowsHotkeyNativeService", "UnixGlobalHotkeyNativeService",
    "WindowsProcessLoopbackNativeService", "UnixProcessLoopbackNativeService",
    "WindowsProcessLoopbackCaptureFactory", "UnixProcessLoopbackCaptureFactory",
    "WindowsNativeDeviceDiscoveryPlatformService", "UnixNativeDeviceDiscoveryPlatformService",
]:
    if marker in registration: PASS.append(f"DI platform implementation {marker}")
    else: FAIL.append(f"DI platform implementation missing: {marker}")

platform = read("src/PublisherStudio.Web/Services/PublisherPlatformRuntimeServices.cs")
for marker in ["IPublisherPlatformRuntimeService", "IsSameOrDescendantPath", "StringComparison.OrdinalIgnoreCase", "StringComparison.Ordinal", "File.SetUnixFileMode"]:
    if marker in platform: PASS.append(f"platform boundary marker {marker}")
    else: FAIL.append(f"platform boundary marker missing: {marker}")

for rel in [
    "docs/index.md", "docs/docfx.json", "docs/toc.yml", "docs/guide/toc.yml", "docs/pdf/toc.yml", "docs/pdf-cover.html",
    "docs/templates/publisherstudio/public/main.css", "docs/templates/publisherstudio/public/main.js",
    "docs/templates/publisherstudio/public/favicon.svg", "docs/templates/publisherstudio/public/logo.svg",
    "build/NodeRuntime.Common.ps1",
]:
    if (ROOT / rel).is_file(): PASS.append(f"source payload {rel}")
    else: FAIL.append(f"source payload missing: {rel}")

build_doc = read("build/Build-Documentation.ps1")
for marker in ["Resolve-PublisherStudioNodeRuntime", "Assert-PublisherStudioGeneratedHtmlPreflight", "--export-tagged-pdf", "--generate-pdf-document-outline", "skipped-preserve-tagging", "html-accessibility-fallback", "htmlPreflightValidated"]:
    if marker in build_doc: PASS.append(f"documentation pipeline marker {marker}")
    else: FAIL.append(f"documentation pipeline missing marker: {marker}")
pages = read(".github/scripts/prepare-pages-artifact.py")
for marker in ["--html-only", "pdfAccessibilityMode", "htmlPreflightValidated", "html-accessibility-fallback"]:
    if marker in pages: PASS.append(f"Pages validation marker {marker}")
    else: FAIL.append(f"Pages validation missing marker: {marker}")

for rel in ["Build-Release.ps1", "Build-LocalDevelopment.ps1"]:
    for marker in ["Assert-CrossPlatformBoundaries.ps1", "Assert-SourcePackagePrerequisites.ps1"]:
        if marker in read(rel): PASS.append(f"{rel} preflight {marker}")
        else: FAIL.append(f"{rel} missing preflight {marker}")
require("Build-Release.ps1", "htmlPreflightValidated", "release documentation HTML preflight assertion")
require("Build-Release.ps1", "pdfAccessibilityMode", "release PDF accessibility mode assertion")

# Cross-platform Python 3 invocation must not assume a `python` executable exists.
python_common = read("build/PythonRuntime.Common.ps1")
for marker in ["'python', 'python3'", "Get-Command py", "PrefixArguments = @('-3')", "Invoke-PublisherStudioPythonScript"]:
    if marker in python_common: PASS.append(f"Python runtime resolver marker {marker}")
    else: FAIL.append(f"Python runtime resolver missing marker: {marker}")
require("build/Assert-SourcePackagePrerequisites.ps1", "Resolve-PublisherStudioPythonRuntime", "release Python preflight")
for rel in [
    "build/Assert-PanelStudioPersistence.ps1",
    "build/Assert-XmlDocumentationCoverage.ps1",
    "build/Invoke-ArchitectureAudit.ps1",
    "build/Assert-IteratorExceptionPolicy.ps1",
    "build/Assert-AsyncContinuationPolicy.ps1",
    "build/Assert-MethodDiagnostics.ps1",
    "build/Assert-ComponentSafety.ps1",
]:
    body = read(rel)
    if "Invoke-PublisherStudioPythonScript" in body and not re.search(r"&\s+python(?:\s|$)", body, re.I):
        PASS.append(f"host-neutral Python invocation {rel}")
    else:
        FAIL.append(f"host-neutral Python invocation missing/unsafe: {rel}")

# DevExpress asset preparation must use the shared cross-platform Node resolver and tolerate empty host-specific candidates.
prepare_assets = read("Prepare-DevExpressAssets.ps1")
for marker in [
    "NodeRuntime.Common.ps1",
    "Resolve-PublisherStudioNodeRuntime",
    "Version '22.23.2'",
    "[AllowEmptyCollection()][string[]]$CandidatePaths = @()",
    "Get-PublisherStudioNodeHostDescriptor",
    "PublisherStudio DevExpress Node.js preflight",
]:
    if marker in prepare_assets: PASS.append(f"DevExpress Node resolver marker {marker}")
    else: FAIL.append(f"DevExpress Node resolver missing marker: {marker}")
for forbidden in ["$programFilesX86 = [Environment]::GetEnvironmentVariable(\"ProgramFiles(x86)\")", "[Parameter(Mandatory = $true)][string[]]$CandidatePaths"]:
    if forbidden not in prepare_assets: PASS.append(f"DevExpress Node resolver excludes legacy binding {forbidden}")
    else: FAIL.append(f"legacy DevExpress Node binding remains: {forbidden}")
source_preflight = read("build/Assert-SourcePackagePrerequisites.ps1")
for marker in ["Resolve-PublisherStudioNodeRuntime", "PublisherStudio Node.js preflight", "Version '22.23.2'"]:
    if marker in source_preflight: PASS.append(f"early Node preflight marker {marker}")
    else: FAIL.append(f"early Node preflight missing marker: {marker}")

# Maintained source must not reintroduce GDI/System.Drawing or obsolete Windows-only common contracts.
maintained = []
for p in WEB.rglob("*"):
    if p.suffix.lower() not in {".cs", ".razor"}: continue
    if "wwwroot/help-docs" in p.as_posix(): continue
    maintained.append(p.read_text(encoding="utf-8-sig"))
joined = "\n".join(maintained)
for token in ["using System.Drawing;", "System.Drawing.", "IWindowsHotkeyNativeService", "IWindowsProcessLoopbackNativeService", "IWindowsProcessLoopbackCaptureFactory"]:
    if token not in joined: PASS.append(f"maintained-source boundary excludes {token}")
    else: FAIL.append(f"maintained-source boundary leak: {token}")


# 3.0.4 compiler findings from the macOS RID-neutral build must stay closed.
native = read("src/PublisherStudio.Web/Services/Streaming/Capture/NativeDeviceDiscoveryPlatformServices.cs")
if "using PublisherStudio.BusinessObjects;" in native and "PublisherRuntimePattern.NativeDirectShowDevice" in native and "PublisherRuntimePattern.NativeAvFoundationDevice" in native:
    PASS.append("native discovery runtime-pattern namespace repair")
else:
    FAIL.append("native discovery runtime-pattern namespace repair missing")
doc_catalog = read("src/PublisherStudio.Web/Services/Documentation/PublisherDocumentationCatalogService.cs")
if "commentCachePath is not null" in doc_catalog and "platform.PathsEqual(commentCachePath, path)" in doc_catalog:
    PASS.append("nullable documentation cache path guard")
else:
    FAIL.append("nullable documentation cache path guard missing")
if "if (!OperatingSystem.IsWindows())" in platform and "File.SetUnixFileMode" in platform:
    PASS.append("Unix file-mode analyzer guard")
else:
    FAIL.append("Unix file-mode analyzer guard missing")

# Ensure the dedicated boundary audit itself succeeds.
result = subprocess.run([sys.executable, str(ROOT / "build/audit_cross_platform_boundaries.py")], cwd=ROOT, capture_output=True, text=True)
if result.returncode == 0: PASS.append("cross-platform boundary audit")
else: FAIL.append("cross-platform boundary audit failed: " + (result.stdout + result.stderr).strip())

if FAIL:
    print("PublisherStudio 3.0.4 static release audit failed:")
    for item in FAIL: print(" -", item)
    raise SystemExit(1)
print(f"PublisherStudio 3.0.4 static release audit passed: {len(PASS)} checks.")

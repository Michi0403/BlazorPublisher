#!/usr/bin/env python3
"""Static guard for PublisherStudio platform boundaries and host-safe path behavior."""
from pathlib import Path
import re, sys

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src" / "PublisherStudio.Web"
failures: list[str] = []
checks = 0

def check(condition: bool, message: str) -> None:
    global checks
    checks += 1
    if not condition:
        failures.append(message)

def text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        return path.read_text(encoding="utf-8")

csproj = text(APP / "PublisherStudio.Web.csproj")
check("System.Drawing.Common" not in csproj, "PublisherStudio.Web must not reference System.Drawing.Common.")
check("System.Security.Cryptography.ProtectedData" not in csproj, "PublisherStudio.Web must not carry the unused ProtectedData package.")

maintained_sources = [p for p in APP.rglob("*") if p.suffix.lower() in {".cs", ".razor"} and "wwwroot/help-docs" not in p.as_posix()]
for p in maintained_sources:
    s = text(p)
    rel = p.relative_to(ROOT).as_posix()
    if re.search(r"\busing\s+System\.Drawing\s*;|\bSystem\.Drawing\.", s):
        failures.append(f"GDI/System.Drawing leak in maintained source: {rel}")
check(not any("GDI/System.Drawing leak" in f for f in failures), "Maintained source must remain free of System.Drawing/GDI APIs.")

allowed_os_branches = {
    "src/PublisherStudio.Web/Program.cs",
    "src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs",
    "src/PublisherStudio.Web/Services/PublisherPlatformRuntimeServices.cs",
    "src/PublisherStudio.Web/Services/Streaming/Capture/WindowsProcessLoopbackCapture.cs",
    "src/PublisherStudio.Web/Services/Streaming/Capture/WindowsProcessLoopbackNativeService.cs",
    "src/PublisherStudio.Web/Services/Streaming/Hotkeys/WindowsHotkeyNativeService.cs",
}
os_pattern = re.compile(r"OperatingSystem\.Is(?:Windows|Linux|MacOS|FreeBSD)|RuntimeInformation\.IsOSPlatform")
leaks = []
for p in maintained_sources:
    rel = p.relative_to(ROOT).as_posix()
    if rel in allowed_os_branches:
        continue
    if os_pattern.search(text(p)):
        leaks.append(rel)
check(not leaks, "OS detection leaked outside composition/platform-specific implementations: " + ", ".join(leaks))

registration = text(APP / "PublisherStudioServiceCollectionExtensions.cs")
for marker in (
    "IPublisherPlatformRuntimeService, WindowsPublisherPlatformRuntimeService",
    "IPublisherPlatformRuntimeService, UnixPublisherPlatformRuntimeService",
    "IGlobalHotkeyNativeService, WindowsHotkeyNativeService",
    "IGlobalHotkeyNativeService, UnixGlobalHotkeyNativeService",
    "IProcessLoopbackNativeService, WindowsProcessLoopbackNativeService",
    "IProcessLoopbackNativeService, UnixProcessLoopbackNativeService",
    "IProcessLoopbackCaptureFactory, WindowsProcessLoopbackCaptureFactory",
    "IProcessLoopbackCaptureFactory, UnixProcessLoopbackCaptureFactory",
    "INativeDeviceDiscoveryPlatformService, WindowsNativeDeviceDiscoveryPlatformService",
    "INativeDeviceDiscoveryPlatformService, UnixNativeDeviceDiscoveryPlatformService",
):
    check(marker in registration, f"Missing host-specific DI registration marker: {marker}")

all_source_text = "\n".join(text(p) for p in maintained_sources)
for obsolete in ("IWindowsHotkeyNativeService", "IWindowsProcessLoopbackNativeService", "IWindowsProcessLoopbackCaptureFactory"):
    check(obsolete not in all_source_text, f"Obsolete Windows-only common interface remains: {obsolete}")

platform_file = text(APP / "Services" / "PublisherPlatformRuntimeServices.cs")
for marker in ("StringComparison.OrdinalIgnoreCase", "StringComparison.Ordinal", "IsSameOrDescendantPath", "File.SetUnixFileMode", "FfmpegUnixInstallPaths"):
    check(marker in platform_file, f"Platform runtime boundary is missing: {marker}")

for rel in (
    "Services/Documentation/PublisherDocumentationCatalogService.cs",
    "Services/Streaming/Lan/LanStreamingServer.cs",
    "Services/Streaming/UseCases/Lan/StreamingLanUseCases.cs",
):
    p = APP / rel
    check(p.is_file() and "IsSameOrDescendantPath" in text(p), f"Path containment must use the platform boundary: {rel}")

check("PublisherStudioPowerShellHost" in csproj and ">pwsh<" in csproj, "The project must select pwsh on non-Windows hosts.")
check("System.IO.Path]::GetFullPath" in csproj and "/../../Prepare-DevExpressAssets.ps1" in csproj, "The project must normalize the DevExpress preparation script path cross-platform.")

doc = text(ROOT / "build" / "Build-Documentation.ps1")
for marker in ("NodeRuntime.Common.ps1", "Resolve-PublisherStudioNodeRuntime", "--export-tagged-pdf", "--generate-pdf-document-outline", "htmlPreflightValidated", "html-accessibility-fallback"):
    check(marker in doc, f"Cross-platform documentation pipeline is missing marker: {marker}")

pages = text(ROOT / ".github" / "scripts" / "prepare-pages-artifact.py")
for marker in ("--html-only", "pdfAccessibilityMode", "htmlPreflightValidated", "html-accessibility-fallback"):
    check(marker in pages, f"Pages validator is missing cross-platform/accessibility marker: {marker}")

for build_file in ("Build-Release.ps1", "Build-LocalDevelopment.ps1"):
    s = text(ROOT / build_file)
    check("Assert-CrossPlatformBoundaries.ps1" in s, f"{build_file} must run the cross-platform boundary guard.")
    check("Assert-SourcePackagePrerequisites.ps1" in s, f"{build_file} must fail fast on incomplete source packages.")

if failures:
    for failure in failures:
        print("ERROR:", failure)
    raise SystemExit(1)
print(f"PublisherStudio cross-platform boundary audit passed: {checks} checks; no platform leaks detected.")

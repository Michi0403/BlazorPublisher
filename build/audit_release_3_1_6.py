#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
errors: list[str] = []


def text(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        errors.append(f"missing file: {rel}")
        return ""
    return path.read_text(encoding="utf-8-sig", errors="replace")


def require(rel: str, needle: str, label: str | None = None) -> None:
    if needle not in text(rel):
        errors.append(label or f"{rel} missing required marker: {needle}")


def forbid(rel: str, needle: str, label: str | None = None) -> None:
    if needle in text(rel):
        errors.append(label or f"{rel} still contains forbidden marker: {needle}")


for rel in ("src/PublisherStudio.Web/PublisherStudio.Web.csproj", "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj"):
    require(rel, "<Version>3.1.6</Version>")
major, minor, patch = (3, 1, 5)
if minor > 9 or patch > 9:
    errors.append("release version violates the one-digit minor/patch slot policy")
require("src/PublisherStudio.Web/package.json", '"version": "3.1.6"')
require("src/PublisherStudio.Web/package-lock.json", '"version": "3.1.6"')
require("RELEASE.md", "CHANGELOG-v3.1.6-LOCALGPT-PACKAGING-101-CONSUMPTION.md")
require("RELEASE.md", "VALIDATION-v3.1.6-source.md")
text("CHANGELOG-v3.1.6-LOCALGPT-PACKAGING-101-CONSUMPTION.md")
text("VALIDATION-v3.1.6-source.md")
require("docs/docfx.json", '"publisherstudioVersion": "3.1.6"')
require("docs/pdf/toc.yml", "PublisherStudio-3.1.6.pdf")

# Retain system-variable ownership repairs.
store = "src/PublisherStudio.Web/Services/Configuration/SystemVariableStoreService.cs"
contract = "src/PublisherStudio.Web/Services/Configuration/ISystemVariableStoreService.cs"
for property_name, key in (
    ("MaximumVideoArchiveEntries", "RuntimePolicy.MaximumVideoArchiveEntries"),
    ("MaximumNotificationMessages", "RuntimePolicy.MaximumNotificationMessages"),
    ("MaximumOrganicPayloadCharacters", "RuntimePolicy.MaximumOrganicPayloadCharacters"),
    ("OrganicReplayMaximumTrackedMessages", "RuntimePolicy.OrganicReplayMaximumTrackedMessages"),
):
    require(contract, f"int {property_name} {{ get; }}")
    require(store, key)
    require(store, f"public int {property_name}")
forbid("src/PublisherStudio.Web/Services/Configuration/OrganicReplayPolicyDataService.cs", 'GetInt("RuntimePolicy.')
forbid("src/PublisherStudio.Web/Services/Configuration/PublisherRuntimePolicyDataService.cs", 'GetInt("RuntimePolicy.')

# PublisherStudio consumes, but no longer owns, LocalGPT.ReleasePackaging source.
if (ROOT / "src" / "LocalGPT.ReleasePackaging").exists():
    errors.append("PublisherStudio still contains duplicate LocalGPT.ReleasePackaging source")
if (ROOT / "build" / "Publish-ReleasePackagingPackage.ps1").exists():
    errors.append("PublisherStudio still contains a local release-packaging package publisher")
ensure_rel = "build/Ensure-ReleasePackagingPackage.ps1"
ensure = text(ensure_rel)
for marker in (
    "Test-ReleasePackagingPackage",
    "$env:LOCALGPT_REPOSITORY",
    "LocalGPT', 'NuGet'",
    "https://github.com/Michi0403/LocalGPT/releases/latest/download/$packageName",
    "--configfile $nugetConfig",
    "Package-source mapping",
):
    if marker.lower() not in ensure.lower():
        errors.append(f"Ensure-ReleasePackagingPackage.ps1 missing shared-package marker: {marker}")
# No active --add-source argument may remain.
for line in ensure.splitlines():
    stripped = line.strip()
    if "--add-source" in stripped and not stripped.startswith("#"):
        errors.append("Ensure-ReleasePackagingPackage.ps1 still actively uses --add-source")
require(ensure_rel, "dotnet tool install LocalGPT.ReleasePackaging")
require(ensure_rel, "| ForEach-Object { Write-Host $_ }")
require(ensure_rel, "Write-Output ([string]$command)")

require(ensure_rel, '[string]$Version = "1.0.1"')
build = text("Build-Release.ps1")
for marker in (
    "$releasePackagingToolOutput = @(",
    "$releasePackagingToolOutput.Count -ne 1",
    "Prepared release-packaging tool is missing",
    "PublisherStudio.InstallerConsole",
):
    if marker not in build:
        errors.append(f"Build-Release.ps1 missing single-value/installer marker: {marker}")
for marker in (
    '[string]$ReleasePackagingVersion = "1.0.1"',
    '[string]$ReleasePackagingPackageUrl = ""',
    '[switch]$RefreshReleasePackagingPackage',
    "LocalGptRepository = $LocalGptRepository",
    "Ensure-ReleasePackagingPackage.ps1",
    "Publish-UnixRuntime",
    "SHA256SUMS.txt",
):
    if marker not in build:
        errors.append(f"Build-Release.ps1 missing shared-package/release marker: {marker}")
for marker in ("build/Ensure-WireProtocolPackage.ps1", "build/Ensure-ReleasePackagingPackage.ps1", "build/NativeReleasePackaging.ps1"):
    require("build/Assert-SourcePackagePrerequisites.ps1", marker)

for rid in ("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"):
    if rid not in build:
        errors.append(f"Build-Release.ps1 missing RID {rid}")

# Debug docs/Pages maintenance remains intact.
require("Directory.Build.targets", "-AllowMissingPdf")
require("build/Update-GitHubPagesSnapshot.ps1", "[switch]$AllowMissingPdf")
require("build/Update-GitHubPagesSnapshot.ps1", "$foundPdfNames = @($versionedDocumentationPdfs | ForEach-Object { $_.Name })")

# Existing InteractiveServer boundaries remain explicit.
for rel in ("Components/Pages/Editor.razor", "Components/Pages/Help.razor", "Components/Pages/Localization.razor", "Components/Pages/OrganicPlugins.razor"):
    require("src/PublisherStudio.Web/" + rel, "@rendermode InteractiveServer", f"InteractiveServer boundary missing from {rel}")

# Maintain direct system-variable key ownership policy.
allowed = {
    "src/PublisherStudio.Web/Program.cs",
    "src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs",
    store,
}
direct_key = re.compile(r'(?:SystemVariables|systemVariables|_systemVariables)\s*\.\s*(?:GetString|GetInt|GetTimeSpan|Set)\s*\(\s*"', re.I | re.M)
for path in (ROOT / "src" / "PublisherStudio.Web").rglob("*"):
    if path.suffix.lower() not in {".cs", ".razor"} or any(part in {"bin", "obj", "Migrations"} for part in path.parts):
        continue
    rel = path.relative_to(ROOT).as_posix()
    if rel not in allowed and direct_key.search(path.read_text(encoding="utf-8-sig", errors="replace")):
        errors.append(f"direct system-variable key remains outside central store: {rel}")

native = text("build/NativeReleasePackaging.ps1")
for marker in (".dmg", ".tar.gz", ".AppImage", ".deb", ".rpm", "hdiutil", "appimagetool", "install-dependencies.sh", "New-MacLauncher", "New-Rpm", "New-AppImage"):
    if marker not in native:
        errors.append(f"NativeReleasePackaging.ps1 missing {marker}")

if errors:
    print("PublisherStudio 3.1.6 static release audit FAILED:")
    for error in errors:
        print(" -", error)
    raise SystemExit(1)
print("PublisherStudio 3.1.6 static release audit passed.")

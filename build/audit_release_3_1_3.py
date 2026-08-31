#!/usr/bin/env python3
from pathlib import Path
import re
import sys

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
    require(rel, "<Version>3.1.3</Version>")
major, minor, patch = (3, 1, 3)
if minor > 9 or patch > 9:
    errors.append("release version violates the one-digit minor/patch slot policy")
require("src/PublisherStudio.Web/package.json", '"version": "3.1.3"')
require("src/PublisherStudio.Web/package-lock.json", '"version": "3.1.3"')
require("RELEASE.md", "CHANGELOG-v3.1.3-BUILD-MAINTENANCE-CROSS-PLATFORM-REVIEW.md")
require("RELEASE.md", "VALIDATION-v3.1.3-source.md")
text("CHANGELOG-v3.1.3-BUILD-MAINTENANCE-CROSS-PLATFORM-REVIEW.md")
text("VALIDATION-v3.1.3-source.md")
require("docs/docfx.json", '"publisherstudioVersion": "3.1.3"')
require("docs/pdf/toc.yml", "PublisherStudio-3.1.3.pdf")

# Reported system-variable initialization failure: only the central store may own raw keys.
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
require("src/PublisherStudio.Web/Services/Configuration/PublisherRuntimePolicyDataService.cs", "systemVariables.MaximumVideoArchiveEntries")
require("src/PublisherStudio.Web/Services/Configuration/PublisherRuntimePolicyDataService.cs", "systemVariables.MaximumNotificationMessages")
require("src/PublisherStudio.Web/Services/Configuration/PublisherRuntimePolicyDataService.cs", "systemVariables.MaximumOrganicPayloadCharacters")
require("src/PublisherStudio.Web/Services/Configuration/OrganicReplayPolicyDataService.cs", "systemVariables.OrganicReplayMaximumTrackedMessages")
forbid("src/PublisherStudio.Web/Services/Configuration/OrganicReplayPolicyDataService.cs", 'GetInt("RuntimePolicy.')
forbid("src/PublisherStudio.Web/Services/Configuration/PublisherRuntimePolicyDataService.cs", 'GetInt("RuntimePolicy.')
require("src/PublisherStudio.Web/Services/Configuration/OrganicReplayPolicyDataService.cs", "/// <value>The snapshot value")

# Mirror the maintained direct-key policy so this release audit catches the two reported findings.
allowed = {
    "src/PublisherStudio.Web/Program.cs",
    "src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs",
    store,
}
direct_key = re.compile(r'(?:SystemVariables|systemVariables|_systemVariables)\s*\.\s*(?:GetString|GetInt|GetTimeSpan|Set)\s*\(\s*"', re.I | re.M)
source_root = ROOT / "src" / "PublisherStudio.Web"
for path in source_root.rglob("*"):
    if path.suffix.lower() not in {".cs", ".razor"} or any(part in {"bin", "obj", "Migrations"} for part in path.parts):
        continue
    rel = path.relative_to(ROOT).as_posix()
    if rel not in allowed and direct_key.search(path.read_text(encoding="utf-8-sig", errors="replace")):
        errors.append(f"direct system-variable key remains outside central store: {rel}")

# Reported XML-documentation gate and shared package tool.
require("src/LocalGPT.ReleasePackaging/Program.cs", "/// <summary>")
require("src/LocalGPT.ReleasePackaging/Program.cs", "internal static class Program")

# Debug documentation/Pages must tolerate intentional no-PDF output while Release remains strict.
require("Directory.Build.targets", "<PublisherStudioPagesPdfArgument Condition=\"'$(RequirePublisherStudioDocumentationPdf)' != 'true'\">-AllowMissingPdf</PublisherStudioPagesPdfArgument>")
require("build/Update-GitHubPagesSnapshot.ps1", "[switch]$AllowMissingPdf")
require("build/Update-GitHubPagesSnapshot.ps1", "$foundPdfNames = @($versionedDocumentationPdfs | ForEach-Object { $_.Name })")
require("build/Update-GitHubPagesSnapshot.ps1", "--html-only")
require("build/Update-GitHubPagesSnapshot.ps1", "pdfAvailable=false")
forbid("build/Update-GitHubPagesSnapshot.ps1", "src\\\\PublisherStudio.Web", "Pages script still contains doubled Windows-only source path separators")
require("Build-Release.ps1", "$versionedPdfDisplay = if ($versionedPdfNames.Count -eq 0) { '<none>' }")
forbid("Build-Release.ps1", "src\\PublisherStudio.Web\\PublisherStudio.Web.csproj", "Build-Release still contains Windows-only source path separators")
forbid("Build-LocalDevelopment.ps1", "src\\PublisherStudio.Web\\PublisherStudio.Web.csproj", "Build-LocalDevelopment still contains Windows-only source path separators")

# Cross-platform release matrix and package formats.
build = text("Build-Release.ps1")
for rid in ("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"):
    if rid not in build:
        errors.append(f"Build-Release.ps1 missing RID {rid}")
for marker in ("Publish-UnixRuntime", "application payload (no setup console)", "SHA256SUMS.txt", "Ensure-ReleasePackagingPackage.ps1"):
    if marker not in build:
        errors.append(f"Build-Release.ps1 missing release marker: {marker}")
require("src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj", "<PackageId>LocalGPT.ReleasePackaging</PackageId>")
native = text("build/NativeReleasePackaging.ps1")
for marker in (".dmg", ".tar.gz", ".AppImage", ".deb", ".rpm", "hdiutil", "appimagetool"):
    if marker not in native:
        errors.append(f"NativeReleasePackaging.ps1 missing {marker}")
if "dpkg-deb" in build or "dpkg-deb" in native:
    errors.append("dpkg-deb remains in active release packaging")

# Baseline InteractiveServer page boundaries supplied by the user.
for rel in ("Components/Pages/Editor.razor", "Components/Pages/Help.razor", "Components/Pages/Localization.razor", "Components/Pages/OrganicPlugins.razor"):
    require("src/PublisherStudio.Web/" + rel, "@rendermode InteractiveServer", f"InteractiveServer boundary missing from {rel}")

if errors:
    print("PublisherStudio 3.1.3 static release audit FAILED:")
    for error in errors:
        print(" -", error)
    raise SystemExit(1)
print("PublisherStudio 3.1.3 static release audit passed.")

\
#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

def text(rel):
    p = ROOT / rel
    if not p.is_file():
        errors.append(f"missing file: {rel}")
        return ""
    return p.read_text(encoding="utf-8-sig", errors="replace")

def require(rel, needle, label=None):
    body = text(rel)
    if needle not in body:
        errors.append(label or f"{rel} missing required marker: {needle}")

def forbid(rel, needle, label=None):
    body = text(rel)
    if needle in body:
        errors.append(label or f"{rel} still contains forbidden marker: {needle}")

for rel in ["src/PublisherStudio.Web/PublisherStudio.Web.csproj", "src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj"]:
    require(rel, "<Version>3.1.2</Version>")
major, minor, patch = (3, 1, 2)
if minor > 9 or patch > 9:
    errors.append("release version violates the one-digit minor/patch slot policy")
require("RELEASE.md", "CHANGELOG-v3.1.2-OPERATOR-POLICY-RELEASE-PACKAGING.md")
require("RELEASE.md", "VALIDATION-v3.1.2-source.md")
text("CHANGELOG-v3.1.2-OPERATOR-POLICY-RELEASE-PACKAGING.md")
text("VALIDATION-v3.1.2-source.md")

# Persisted replay/operator policy.
forbid("src/PublisherStudio.Web/Services/Configuration/OrganicReplayPolicyDataService.cs", "MaximumTrackedMessages = 4096", "old 4096 replay ceiling remains")
require("src/PublisherStudio.Web/Services/Configuration/OrganicReplayPolicyDataService.cs", "RuntimePolicy.OrganicReplayMaximumTrackedMessages")
require("src/PublisherStudio.Web/Services/Configuration/OrganicReplayPolicyDataService.cs", "int.MaxValue")
require("src/PublisherStudio.Web/Services/Configuration/SystemVariableStoreService.cs", "RuntimePolicy.OrganicReplayMaximumTrackedMessages")
require("src/PublisherStudio.Web/Services/Configuration/SystemVariableStoreService.cs", "_organicReplayMaximumTrackedMessagesName] = int.MaxValue")
require("src/PublisherStudio.Web/Services/Configuration/PublisherRuntimePolicyDataService.cs", 'GetInt("RuntimePolicy.MaximumOrganicPayloadCharacters", int.MaxValue)')

# Release packaging.
build = text("Build-Release.ps1")
for rid in ["win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"]:
    if rid not in build: errors.append(f"Build-Release.ps1 missing RID {rid}")
require("Build-Release.ps1", "Publish-UnixRuntime")
require("Build-Release.ps1", "application payload (no setup console)")
require("Build-Release.ps1", "SHA256SUMS.txt")
require("Build-Release.ps1", "Ensure-ReleasePackagingPackage.ps1")
require("src/LocalGPT.ReleasePackaging/LocalGPT.ReleasePackaging.csproj", "<PackageId>LocalGPT.ReleasePackaging</PackageId>")
require("src/LocalGPT.ReleasePackaging/Program.cs", '"control.tar.gz"')
require("src/LocalGPT.ReleasePackaging/Program.cs", "SHA256.HashData")
native = text("build/NativeReleasePackaging.ps1")
for marker in [".dmg", ".tar.gz", ".AppImage", ".deb", ".rpm", "hdiutil", "appimagetool", "rpmbuild"]:
    if marker not in native: errors.append(f"NativeReleasePackaging.ps1 missing {marker}")
if "dpkg-deb" in build or "dpkg-deb" in native:
    errors.append("dpkg-deb remains in active release packaging")

# Reviewed InteractiveServer boundaries from supplied baseline.
for rel in ["Components/Pages/Editor.razor", "Components/Pages/Help.razor", "Components/Pages/Localization.razor", "Components/Pages/OrganicPlugins.razor"]:
    require("src/PublisherStudio.Web/" + rel, "@rendermode InteractiveServer", f"InteractiveServer boundary missing from {rel}")

if errors:
    print("PublisherStudio 3.1.2 static release audit FAILED:")
    for e in errors: print(" -", e)
    sys.exit(1)
print("PublisherStudio 3.1.2 static release audit passed.")

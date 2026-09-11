from pathlib import Path
import json
import plistlib
import re
import sys

root = Path(__file__).resolve().parents[1]
failures = []

def read(relative):
    path = root / relative
    if not path.is_file():
        failures.append(f"missing file: {relative}")
        return ""
    return path.read_text(encoding="utf-8-sig")

def require(relative, token, label=None):
    text = read(relative)
    if token not in text:
        failures.append(label or f"{relative}: missing {token!r}")
    return text

def version(relative, expected):
    text = require(relative, f"<Version>{expected}</Version>")
    match = re.search(r"<Version>(\d+)\.(\d+)\.(\d+)</Version>", text)
    if not match:
        failures.append(f"{relative}: semantic version not found")
        return
    major, minor, patch = map(int, match.groups())
    if minor > 9 or patch > 9:
        failures.append(f"{relative}: version {major}.{minor}.{patch} violates the one-digit minor/patch release rule")

version("src/PublisherStudio.Web/PublisherStudio.Web.csproj", "3.6.0")
version("src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj", "3.6.0")
for relative, token in (
    ("src/PublisherStudio.Web/package.json", '"version": "3.6.0"'),
    ("src/PublisherStudio.Web/package-lock.json", '"version": "3.6.0"'),
    ("docs/docfx.json", '"publisherstudioVersion": "3.6.0"'),
    ("docs/pdf-cover.html", "PublisherStudio 3.6.0 Documentation"),
    ("docs/index.md", "**Version 3.6.0**"),
    ("CHANGELOG-v3.6.0-MACOS-SIGNING-PDF-RENDER-LATENCY-REPAIR.md", "3.6.0"),
    ("VALIDATION-v3.6.0-source.md", "3.6.0"),
    ("RELEASE.md", "PublisherStudio 3.6.0"),
):
    require(relative, token)

entitlements_path = root / "build/assets/mac-apphost-entitlements.plist"
if not entitlements_path.is_file():
    failures.append("macOS apphost entitlements asset is missing")
else:
    try:
        with entitlements_path.open("rb") as stream:
            entitlements = plistlib.load(stream)
        if entitlements != {"com.apple.security.cs.allow-jit": True}:
            failures.append(f"unexpected macOS apphost entitlements: {entitlements!r}")
    except Exception as exc:
        failures.append(f"macOS apphost entitlements are not parseable: {exc}")

trust = read("build/Initialize-MacReleaseTrust.ps1")
for token in (
    "'security','codesign','pkgbuild','hdiutil','xcrun','plutil'",
    "assets/mac-apphost-entitlements.plist",
    "& $plutil -lint $entitlementsSourcePath",
    "macOS apphost entitlements preflight passed",
):
    if token not in trust:
        failures.append(f"Initialize-MacReleaseTrust.ps1: missing early entitlement guard {token!r}")

native = read("build/NativeReleasePackaging.ps1")
for token in (
    "$entitlementsSourcePath = Join-Path $PSScriptRoot 'assets/mac-apphost-entitlements.plist'",
    "& $plutil -convert xml1 -o $entitlementsPath $entitlementsSourcePath",
    "& $plutil -lint $entitlementsPath",
    "@('--entitlements',$entitlementsPath)",
):
    if token not in native:
        failures.append(f"NativeReleasePackaging.ps1: missing normalized entitlement signing token {token!r}")
if "$entitlements = @'" in native:
    failures.append("NativeReleasePackaging.ps1: inline PowerShell entitlement XML generation remains")

build_docs = read("build/Build-Documentation.ps1")
for token in (
    "$pdfCompletedBeforeProcessExit = $false",
    "$liveStableLengthChecks -ge 4",
    "Test-PublisherStudioCompletePdf -Path $PdfPath -MinimumBytes $MinimumBytes",
    "the lingering renderer was terminated after PDF validation",
):
    if token not in build_docs:
        failures.append(f"Build-Documentation.ps1: missing live PDF completion token {token!r}")
if "$process.WaitForExit($browserPdfTimeoutMilliseconds)" in build_docs:
    failures.append("Build-Documentation.ps1: successful browser PDF rendering can still block on the full timeout")

# Preserve 3.5.8/3.5.9 logging and documentation contracts without changing the baseline.
file_logger = read("src/PublisherStudio.Web/Services/Logging/FileLogger.cs")
provider = read("src/PublisherStudio.Web/Services/Logging/FileLoggerProvider.cs")
baseline_path = root / "build/logging-baseline.json"
try:
    baseline = json.loads(baseline_path.read_text(encoding="utf-8-sig"))["files"]
except Exception as exc:
    failures.append(f"logging baseline could not be read: {exc}")
    baseline = {}
for relative, text in (("src/PublisherStudio.Web/Services/Logging/FileLogger.cs", file_logger),("src/PublisherStudio.Web/Services/Logging/FileLoggerProvider.cs", provider)):
    expected = baseline.get(relative)
    if expected is None:
        failures.append(f"logging baseline missing maintained file: {relative}")
        continue
    actual = {
        "loggerReferences": len(re.findall(r"\bILogger(?:<[^>]+>)?\b", text)),
        "logCalls": len(re.findall(r"\.Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(", text)),
        "catchBlocks": len(re.findall(r"\bcatch\b", text)),
    }
    for metric, minimum in expected.items():
        if actual[metric] < int(minimum):
            failures.append(f"{relative}: {metric} {actual[metric]} is below maintained baseline {minimum}")
if "internal sealed class FileLoggerSharedSink" not in file_logger or file_logger.count("new Thread(") != 1:
    failures.append("shared single-writer logging contract regressed")

installer_program = read("src/PublisherStudio.InstallerConsole/Program.cs")
for token in ("ExtractZipWithFallback(zipPath, targetPath, logger);", "ExtractZipWithFallback(setupZipPath, targetPath, logger);"):
    if token not in installer_program:
        failures.append(f"overlay installer contract regressed: missing {token!r}")
for stale_parameter in ("applicationZipPath", "setupZipPath", "targetPath", "runtimeIdentifier"):
    method_pos = installer_program.find("private static void StopInstalledPublisherStudioForUpdate(")
    doc_pos = installer_program.rfind("/// <summary>", 0, method_pos)
    if method_pos >= 0 and doc_pos >= 0 and f'<param name="{stale_parameter}">' in installer_program[doc_pos:method_pos]:
        failures.append(f"3.5.9 installer XML docs retain stale parameter {stale_parameter}")
for parameter in ("installRoot", "runtimeFolderName", "logger"):
    method_pos = installer_program.find("private static void StopInstalledPublisherStudioForUpdate(")
    doc_pos = installer_program.rfind("/// <summary>", 0, method_pos)
    if method_pos < 0 or doc_pos < 0 or f'<param name="{parameter}">' not in installer_program[doc_pos:method_pos]:
        failures.append(f"3.5.9 installer XML docs missing current parameter {parameter}")

release_build = read("Build-Release.ps1")
preflight = release_build.find("Preflighting the PublisherStudio installer compile before expensive documentation and native packaging...")
documentation = release_build.find("Prepare-PublisherStudioDocumentation", preflight)
if preflight < 0 or documentation < 0 or preflight > documentation:
    failures.append("installer compile preflight must remain before documentation generation")

if failures:
    print("PublisherStudio 3.6.0 release audit FAILED:")
    print("\n".join(f" - {failure}" for failure in failures))
    sys.exit(1)
print("PublisherStudio 3.6.0 release audit passed: macOS entitlements are checked-in/plutil-validated before expensive work, live PDF completion avoids the normal 480-second renderer wait, and logging/installer/XML-documentation protections remain intact.")

#!/usr/bin/env python3
"""Source-only release checks for PublisherStudio 3.5.6."""
from __future__ import annotations

import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION = "3.5.6"
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
    ("CHANGELOG-v3.5.6-INSTALLER-WORKFLOW-GUARD-REPAIR.md", "# PublisherStudio 3.5.6"),
    ("VALIDATION-v3.5.6-source.md", "# PublisherStudio 3.5.6 source validation"),
):
    require(rel, needle)

# Preserve the 3.5.0 PowerShell 5.1 documentation-cache and Debug-PDF repairs.
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

# Preserve the 3.4.9 Organic wording/localization repair.
page_rel = "src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor"
page = read(page_rel)
for needle in ("Back to PublisherStudio", "PublisherStudio organic capabilities"):
    if needle not in page:
        fail(f"{page_rel}: missing corrected UI wording {needle!r}")
if ">Back to Publisher</button>" in page or "<h2>PublisherStudio organ capabilities</h2>" in page:
    fail(f"{page_rel}: obsolete UI naming remains")


# 3.5.6 frontend asset/runtime synchronization and race repair.
app = read("src/PublisherStudio.Web/Components/App.razor")
for asset in ("css/site.css", "js/localizationRuntime.js", "js/videoEffectRuntime.js", "js/componentRuntime.js", "js/publisherInterop.js"):
    if f'{asset}?v={VERSION}' not in app:
        fail(f"App.razor: {asset} does not use the current release cache identity {VERSION}")
if "?v=3.3.5" in app:
    fail("App.razor: stale 3.3.5 browser cache identity remains")
for rel in (
    "src/PublisherStudio.Web/Components/Pages/Editor.razor",
    "src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor",
    "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
):
    text = read(rel)
    if "mediaStudioInterop.js?v=3.3.5" in text or 'publisherInterop.js?v=3.3.5' in text:
        fail(f"{rel}: stale browser module cache identity remains")


for rel in (
    "src/PublisherStudio.Web/Components/Editor/PageSurface.razor",
    "src/PublisherStudio.Web/Components/Editor/WordArtPathEditor.razor",
    "src/PublisherStudio.Web/Components/Editor/BarcodeEditor.razor",
):
    text = read(rel)
    if '"./js/publisherInterop.js?v=3.5.6"' not in text:
        fail(f"{rel}: publisherInterop dynamic import is not release-versioned")

publisher_js = read("src/PublisherStudio.Web/wwwroot/js/publisherInterop.js")
for marker in (
    "target.closest('.dxreRoot, .dx-widget, [class*=\"dxbl-\"], [data-dxbl-loaded]')",
    "Do not synthesize a global resize here",
    "void layoutChanged;",
):
    if marker not in publisher_js:
        fail(f"publisherInterop.js: frontend race guard missing {marker!r}")
editor = read("src/PublisherStudio.Web/Components/Pages/Editor.razor")
if 'id="picture-file-input" class="hidden-file-input"' not in editor or 'multiple OnChange="InsertPicture"' not in editor:
    fail("Editor.razor: multi-picture insertion input is not enabled")
if "args.GetMultipleFiles(64)" not in editor:
    fail("Editor.razor: picture insertion does not consume multiple selected files")
if editor.count('@bind:culture="CultureInfo.InvariantCulture"') < 4:
    fail("Editor.razor: website quality range controls are not invariant-culture bound")

# 3.5.6 Windows updater must stop only the installed runtime before extraction.
installer = read("src/PublisherStudio.InstallerConsole/Program.cs")
for marker in (
    "StopInstalledPublisherStudioForUpdate(targetPath, runtimeFolderName, logger);",
    'Path.Combine(installRoot, runtimeFolderName, "PublisherStudio.Web.exe")',
    'root.TryGetProperty("ExecutablePath", out var executableElement)',
    'Process.GetProcessesByName("PublisherStudio.Web")',
    "PathsEqual(process.MainModule?.FileName, expectedExecutablePath)",
    "process.Kill(entireProcessTree: true)",
    "Removed the installed PublisherStudio runtime endpoint before update extraction.",
):
    if marker not in installer:
        fail(f"InstallerConsole Program.cs: Windows updater ownership guard missing {marker!r}")

# 3.5.6 release parser and source identity prevent stale artifacts.
build_release_rel = "Build-Release.ps1"
build_release = read(build_release_rel)
for needle in (
    "function Get-ReleaseSourceFingerprint",
    "function Initialize-ReleaseArtifactSourceIdentity",
    "PublisherStudio-$Version-SOURCE-SHA256.txt",
    "$script:releaseSourceFingerprint",
    "Published PublisherStudio assembly identity mismatch",
    "RELEASE-VERSION.txt",
    "SOURCE-SHA256.txt",
):
    if needle not in build_release:
        fail(f"{build_release_rel}: missing stale-artifact prevention {needle!r}")

# PowerShell interpolation followed by ':' must delimit the variable name explicitly.
if 'Published PublisherStudio assembly identity mismatch for $Rid ${mode}: expected' not in build_release:
    fail(f"{build_release_rel}: release identity error text must use ${{mode}}: so pwsh can parse it")
for match in re.finditer(r"\$([A-Za-z_][A-Za-z0-9_]*):", build_release):
    if match.group(1).lower() not in {"script", "env", "global", "local", "private", "using"}:
        fail(f"{build_release_rel}: ambiguous PowerShell variable interpolation remains: {match.group(0)!r}")

native_rel = "build/NativeReleasePackaging.ps1"
native = read(native_rel)
for needle in (
    'EXPECTED_VERSION="__VERSION__"',
    'RUNTIME_VERSION_FILE="$APP/RELEASE-VERSION.txt"',
    'RUNTIME_SOURCE_FILE="$APP/SOURCE-SHA256.txt"',
    "verify_bundle_identity()",
    '[ "${#runtime_source}" -eq 64 ]',
    'endpoint_version=$(sed -nE',
    'endpoint_executable=$(sed -nE',
    'Rejecting stale installed runtime endpoint',
    'Preserving alternate runtime rendezvous endpoint',
    'packaged_owner=0',
    'installed_owner=0',
    "terminate_stale_processes()",
    'APP_LOG_FILE="$USER_DATA_DIR/PublisherStudio.log"',
    'cd "$USER_DATA_DIR/runtime"',
    'export PWD="$USER_DATA_DIR/runtime"',
    'export LoggingCore__FileCore__FilePath="$APP_LOG_FILE"',
    "$preinstall = $lifecycleCommon",
    "stop_running_product",
    'owner_command=$(/bin/ps -p "$owner_pid" -o command= 2>/dev/null || true)',
    "remove_runtime_endpoint",
    "--scripts', $pkgScripts",
    "Get-ExternalCommandPath 'productbuild'",
    "$productBuildArguments = @('--package', $componentPackage)",
    "--expand-full $Destination $expandedPackage",
    "-showChoicesXML",
    "Installer could not read the final distribution PKG",
    "Created, expanded, and Installer-read the final macOS distribution PKG successfully",
    "/bin/launchctl asuser",
):
    if needle not in native:
        fail(f"{native_rel}: missing macOS lifecycle contract {needle!r}")

program_rel = "src/PublisherStudio.Web/Program.cs"
for needle in (
    "PublisherApplicationDataPaths.RepairInvalidCurrentDirectory()",
    "PublisherStudio runtime identity: assembly={AssemblyVersion}; executable={ExecutablePath}; base={BaseDirectory}; workingDirectory={WorkingDirectory}.",
    "Environment.ProcessPath ?? \"unknown\"",
    "public static string ResolveProcessWorkingDirectory()",
    "public static string ResolveSafeCurrentDirectory()",
    "public static bool RepairInvalidCurrentDirectory()",
):
    require(program_rel, needle)

endpoint_rel = "src/PublisherStudio.Web/Services/ApplicationHostServices.cs"
for needle in (
    "Version = semanticVersion",
    "ExecutablePath = Environment.ProcessPath ?? string.Empty",
):
    require(endpoint_rel, needle)

localization_dir = ROOT / "src/PublisherStudio.Web/Localization"
catalog_paths = sorted(localization_dir.glob("*.json"))
if not catalog_paths:
    fail("no PublisherStudio localization catalogs found")
else:
    catalogs: dict[str, dict[str, object]] = {}
    for path in catalog_paths:
        try:
            value = json.loads(path.read_text(encoding="utf-8-sig"))
            if not isinstance(value, dict):
                raise ValueError("catalog root is not an object")
            catalogs[path.name] = value
        except (OSError, json.JSONDecodeError, ValueError) as exc:
            fail(f"{path.relative_to(ROOT)}: invalid localization JSON: {exc}")
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

# Preserve routed InteractiveServer ownership; Error stays intentionally static.
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
    if path.is_dir() and path.name in {"bin", "obj", "__pycache__"}:
        fail(f"compiled/generated directory present: {path.relative_to(ROOT)}")
        break
    if path.is_file() and path.suffix in {".pyc", ".pyo"}:
        fail(f"generated Python bytecode present: {path.relative_to(ROOT)}")
        break

# 3.5.6 durable diagnostics, transactional install, and Windows port fallback.
installer = read("src/PublisherStudio.InstallerConsole/Program.cs")
for marker in (
    'Path.Combine(localAppData, "PublisherStudio")',
    'PublisherStudio.Setup.log',
    'SetupFileLoggerProvider',
    'InstallReleaseArchivesTransactionally(',
    'ReadStagedReleaseIdentity(',
    'PublisherStudio.Setup.dll',
    'Staged PublisherStudio application/setup release identities do not match',
    'Directory.Move(installedWrapper, backupWrapper)',
    'PublisherStudio transactional installation failed; restoring the previous wrappers where possible.',
):
    if marker not in installer:
        fail(f"InstallerConsole Program.cs: 3.5.6 install/logging contract missing {marker!r}")
if 'Staged PublisherStudio {role} release stamp' not in installer:
    fail("InstallerConsole Program.cs: staged release identity diagnostics missing")
if 'SetupSemanticVersion, "application"' in installer or 'SetupSemanticVersion, "setup"' in installer:
    fail("InstallerConsole Program.cs: incoming release must not be forced to equal the currently running setup version")

setup_logger = read("src/PublisherStudio.InstallerConsole/Helper/SetupFileLoggerProvider.cs")
for marker in ('File.AppendAllText', 'if (exception is not null)', 'builder.AppendLine().Append(exception)'):
    if marker not in setup_logger:
        fail(f"SetupFileLoggerProvider.cs: durable full-exception logging missing {marker!r}")

program = read("src/PublisherStudio.Web/Program.cs")
for marker in ('TryAppendBootstrapDiagnostic', 'PublisherStudio.log', 'Console.Error.WriteLine(exception)'):
    if marker not in program:
        fail(f"PublisherStudio.Web Program.cs: bootstrap/fatal logging missing {marker!r}")
file_logger = read("src/PublisherStudio.Web/Services/Logging/FileLogger.cs")
if 'PublisherApplicationDataPaths.ResolveUserPath("PublisherStudio.log")' not in file_logger:
    fail("PublisherStudio FileLogger does not default to the durable per-user PublisherStudio.log")

port_service = read("src/PublisherStudio.Web/Services/ApplicationHostServices.cs")
for marker in (
    'ResolveAvailableLoopbackPort',
    'new TcpListener(IPAddress.Loopback, requestedPort)',
    'catch (SocketException exception) when (requestedPort > 0)',
    'new TcpListener(IPAddress.Loopback, 0)',
    'selecting an OS-assigned loopback port instead',
):
    if marker not in port_service:
        fail(f"ApplicationHostServices.cs: Windows loopback fallback missing {marker!r}")

if ERRORS:
    print("PublisherStudio 3.5.6 source audit FAILED:")
    for error in ERRORS:
        print(f" - {error}")
    sys.exit(1)
print(
    f"PublisherStudio {VERSION} source audit passed: {route_count} routed pages, "
    f"{interactive_count} InteractiveServer routed boundaries, {len(catalog_paths)} localization catalogs, "
    "PowerShell documentation/release parsing and macOS runtime/update rendezvous ownership guarded."
)

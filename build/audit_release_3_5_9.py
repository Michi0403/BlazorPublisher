from pathlib import Path
import json
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


version("src/PublisherStudio.Web/PublisherStudio.Web.csproj", "3.5.9")
version("src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj", "3.5.9")
require("src/PublisherStudio.Web/package.json", '"version": "3.5.9"')
require("src/PublisherStudio.Web/package-lock.json", '"version": "3.5.9"')
require("docs/docfx.json", '"publisherstudioVersion": "3.5.9"')
require("CHANGELOG-v3.5.9-XML-DOCUMENTATION-CONTRACT-REPAIR.md", "3.5.9")
require("VALIDATION-v3.5.9-source.md", "3.5.9")
require("RELEASE.md", "PublisherStudio 3.5.9")

setup_project = read("src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj")
if "<ImplicitUsings>enable</ImplicitUsings>" in setup_project:
    failures.append("InstallerConsole unexpectedly enables implicit usings; the explicit compile-surface check needs review")
setup_logger = read("src/PublisherStudio.InstallerConsole/Helper/SetupFileLoggerProvider.cs")
for token in (
    "using System;",
    "using System.IO;",
    "using System.Text;",
    "public IDisposable? BeginScope<TState>(TState state) where TState : notnull",
    "Exception? exception",
    "Func<TState, Exception?, string> formatter",
    "Path.GetFullPath(logPath)",
    "Directory.CreateDirectory(directory)",
    "File.AppendAllText(",
):
    if token not in setup_logger:
        failures.append(f"SetupFileLoggerProvider.cs: missing compile-surface token {token!r}")

file_logger = read("src/PublisherStudio.Web/Services/Logging/FileLogger.cs")
provider = read("src/PublisherStudio.Web/Services/Logging/FileLoggerProvider.cs")
for token in (
    "internal sealed class FileLoggerSharedSink",
    "private readonly BlockingCollection<string> logQueue",
    "logQueue.TryAdd(builder.ToString());",
):
    if token not in file_logger:
        failures.append(f"FileLogger.cs: shared single-writer logging token missing: {token!r}")
if file_logger.count("new Thread(") != 1:
    failures.append("FileLogger.cs: exactly one shared writer thread construction is required")
for token in (
    "sink = new FileLoggerSharedSink(options);",
    "PublisherStudio file logger provider initialization failed",
    "catch (Exception exception)",
):
    if token not in provider:
        failures.append(f"FileLoggerProvider.cs: missing provider initialization diagnostic token {token!r}")

baseline_path = root / "build/logging-baseline.json"
try:
    baseline = json.loads(baseline_path.read_text(encoding="utf-8-sig"))["files"]
except Exception as exc:
    failures.append(f"logging baseline could not be read: {exc}")
    baseline = {}

for relative, text in (
    ("src/PublisherStudio.Web/Services/Logging/FileLogger.cs", file_logger),
    ("src/PublisherStudio.Web/Services/Logging/FileLoggerProvider.cs", provider),
):
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

story = read("src/PublisherStudio.Web/Components/Editor/StoryEditor.razor")
for token in ("if (!Visible || _storyLayoutAttached) return;", "_storyLayoutAttached = false;"):
    if token not in story:
        failures.append(f"StoryEditor render-affinity repair missing: {token!r}")
if story.count("_storyLayoutAttached = true;") != 1:
    failures.append("StoryEditor must attach its layout bridge exactly once in the successful bridge path")

async_audit = read("build/audit_async_continuations.py")
if "Renderer/circuit-affine component continuations must use ConfigureAwait(true)." not in async_audit:
    failures.append("renderer-affinity audit policy was removed")

installer_program = read("src/PublisherStudio.InstallerConsole/Program.cs")
for token in (
    "ExtractZipWithFallback(zipPath, targetPath, logger);",
    "ExtractZipWithFallback(setupZipPath, targetPath, logger);",
):
    if token not in installer_program:
        failures.append(f"maintained overlay installer contract missing: {token!r}")
for forbidden in (
    "InstallReleaseArchivesTransactionally(",
    "ValidateStagedAssemblyVersion(",
    "ReadStagedReleaseIdentity(",
    "rollback-capable transaction",
    "transactional wrapper replacement",
    "whole-directory replacement workflow",
):
    if forbidden in installer_program:
        failures.append(f"unapproved transactional deployment dialect remains: {forbidden}")

release_build = read("Build-Release.ps1")
preflight = release_build.find("Preflighting the PublisherStudio installer compile before expensive documentation and native packaging...")
documentation = release_build.find("Prepare-PublisherStudioDocumentation", preflight)
if preflight < 0 or documentation < 0 or preflight > documentation:
    failures.append("Build-Release.ps1: installer compile preflight must remain before documentation generation")

# 3.5.9 documentation-maintenance regression guards.
program_lines = installer_program.splitlines()
method_index = next((i for i, line in enumerate(program_lines) if "private static void StopInstalledPublisherStudioForUpdate(" in line), -1)
if method_index < 0:
    failures.append("StopInstalledPublisherStudioForUpdate method missing")
else:
    doc_start = method_index
    while doc_start > 0 and program_lines[doc_start - 1].lstrip().startswith("///"):
        doc_start -= 1
    method_docs = "\n".join(program_lines[doc_start:method_index])
    for parameter in ("installRoot", "runtimeFolderName", "logger"):
        if f'<param name="{parameter}">' not in method_docs:
            failures.append(f"StopInstalledPublisherStudioForUpdate XML docs missing current parameter {parameter}")
    for stale_parameter in ("applicationZipPath", "setupZipPath", "targetPath", "runtimeIdentifier"):
        if f'<param name="{stale_parameter}">' in method_docs:
            failures.append(f"StopInstalledPublisherStudioForUpdate XML docs retain stale parameter {stale_parameter}")

for relative, required_summary_token in (
    ("src/PublisherStudio.Web/Services/Logging/FileLogger.cs", "Completes the shared log queue, gives the provider-owned writer thread an opportunity to drain pending entries"),
    ("src/PublisherStudio.Web/Services/Logging/FileLoggerProvider.cs", "Disposes the provider-owned shared file sink so queued entries finish through the single writer"),
):
    if required_summary_token not in read(relative):
        failures.append(f"{relative}: 3.5.9 logging lifetime documentation is missing")

if failures:
    print("PublisherStudio 3.5.9 release audit FAILED:")
    print("\n".join(f" - {failure}" for failure in failures))
    sys.exit(1)

print("PublisherStudio 3.5.9 release audit passed: installer compile surface, one-digit version rule, maintained logging baseline, shared single-writer sink, renderer-affinity guard, overlay installer contract, and early installer preflight are intact.")

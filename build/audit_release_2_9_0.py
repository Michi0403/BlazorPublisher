#!/usr/bin/env python3
"""Static source audit for PublisherStudio 2.9.0 recovery/WebM insertion repair."""
from __future__ import annotations
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
checks = 0

def read(rel: str) -> str:
    path = root / rel
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding="utf-8-sig", errors="replace")

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
    require("Directory.Build.props", "<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>")

    require("src/PublisherStudio.Web/Components/App.razor",
            "css/site.css?v=20260818-290",
            "videoEffectRuntime.js?v=2.9.0",
            "publisherInterop.js?v=2.9.0")
    for rel in (
        "src/PublisherStudio.Web/Components/Pages/Editor.razor",
        "src/PublisherStudio.Web/Components/Editor/MediaStudio.razor",
        "src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor",
    ):
        require(rel, "mediaStudioInterop.js?v=2.9.0")

    editor = "src/PublisherStudio.Web/Components/Pages/Editor.razor"
    require(editor,
            "private readonly object _recoveryCancellationSync = new();",
            "private async Task DebouncedRecoverySaveAsync(CancellationToken cancellationToken)",
            "new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)",
            "Task.WhenAny(delayTask, cancellationSignal.Task).ConfigureAwait(false)",
            "previousCancellation?.Cancel();",
            "cancellation.Dispose();",
            "lock (_recoveryCancellationSync)")
    forbid(editor,
           "DebouncedRecoverySaveAsync(CancellationTokenSource cancellation)",
           "Task.Delay(1800, cancellation.Token)",
           "cancellation.Token.IsCancellationRequested")

    converter = "src/PublisherStudio.Web/Components/Editor/MediaConverterStudio.razor"
    require(converter,
            "private readonly object _pollingSync = new();",
            "var polling = new CancellationTokenSource();",
            "pollingToken = polling.Token;",
            "await PollAsync(pollingToken).ConfigureAwait(false);",
            "if (ReferenceEquals(_polling, polling))",
            "polling.Dispose();",
            "Task.WhenAny(delayTask, cancellationSignal.Task).ConfigureAwait(false)",
            "_polling?.Cancel();")
    forbid(converter,
           "_ => PollAsync(_polling.Token)",
           "Task.Delay(500, cancellationToken)",
           "_polling?.Dispose();",
           "await _polling.CancelAsync()")

    conversion_service = "src/PublisherStudio.Web/Services/MediaConversion/MediaConversionService.cs"
    require(conversion_service,
            "var executionToken = linked.Token;",
            "ExecuteAsync(job, capabilities.Executable, executionToken)",
            "private async Task ExecuteAsync(JobState job, string executable, CancellationToken cancellationToken)",
            "process.StandardOutput.ReadLineAsync(cancellationToken)",
            "process.StandardError.ReadLineAsync(cancellationToken)",
            "process.WaitForExitAsync(cancellationToken)",
            "finally\n        {\n            job.Cancellation.Dispose();",
            "if (job.Status is MediaConversionJobStatus.Completed or MediaConversionJobStatus.Failed or MediaConversionJobStatus.Cancelled)")
    forbid(conversion_service,
           "ReadLineAsync(job.Cancellation.Token)",
           "WaitForExitAsync(job.Cancellation.Token)",
           "foreach (var job in _jobs.Values) job.Cancellation.Dispose();")

    # Heuristic source rule: async work receives CancellationToken, while source ownership stays with the caller.
    source_root = root / "src/PublisherStudio.Web"
    risky = []
    async_cts = re.compile(
        r"\basync\s+(?:Task(?:<[^>]+>)?|ValueTask(?:<[^>]+>)?)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\([^)]*CancellationTokenSource[^)]*\)",
        re.S,
    )
    for path in source_root.rglob("*"):
        if path.suffix not in {".cs", ".razor"}:
            continue
        data = path.read_text(encoding="utf-8-sig", errors="replace")
        for match in async_cts.finditer(data):
            risky.append(f"{path.relative_to(source_root).as_posix()}:{match.group(1)}")
    checks += 1
    if risky:
        raise AssertionError(f"async methods still accept CancellationTokenSource directly: {risky}")

    media_js = "src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js"
    require(media_js,
            "function readEbmlVint(bytes, offset, maximumWidth, keepMarker)",
            "function encodeEbmlSize(value, preferredWidth = 0)",
            "function locateWebmInfo(bytes)",
            "function webmTimecodeScaleAndDuration(bytes, info)",
            "async function webmBlobWithDuration(blob, durationSeconds, mimeType)",
            "elementId.value === 0x2ad7b1",
            "elementId.value === 0x4489",
            "new DataView(durationElement.buffer).setFloat64(3, durationTicks, false)",
            "const embeddedBlob = await webmBlobWithDuration(blob, state.retainedRecordingInfo?.durationSeconds, mimeType);",
            "Math.ceil(embeddedBlob.size / RECORDING_TRANSFER_CHUNK_SIZE)",
            "embeddedBlob.slice(start, end).arrayBuffer()",
            "MediaRecordingTransferProgress', transferred, embeddedBlob.size")
    # Direct download deliberately remains based on the original retained browser Blob/object URL.
    require(media_js,
            "const blob = state.retainedRecordingBlob;",
            "anchor.href = state.retainedRecordingUrl || URL.createObjectURL(blob);")

    expected_modes = {
        "src/PublisherStudio.Web/Components/Pages/Editor.razor": "@rendermode InteractiveServer",
        "src/PublisherStudio.Web/Components/Pages/Help.razor": "@rendermode InteractiveServer",
        "src/PublisherStudio.Web/Components/Pages/Localization.razor": "@rendermode InteractiveServer",
        "src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor": "@rendermode InteractiveServer",
        "src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor": "@rendermode @(new InteractiveServerRenderMode(prerender: false))",
    }
    components = root / "src/PublisherStudio.Web/Components"
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

    require("CHANGELOG-v2.9.0-RECOVERY-CANCELLATION-WEBM-EMBED-REPAIR.md",
            "CancellationTokenSource", "Duration", "3828x1962", "DevExpress 25.2.9")
    require("VALIDATION-v2.9.0-source.md",
            "No `dotnet`", "zero async C#/Razor methods", "14.800-second duration")

    print(f"PublisherStudio 2.9.0 recovery/WebM source audit passed: {checks} checks.")
except Exception as exc:
    print(f"PublisherStudio 2.9.0 recovery/WebM source audit failed: {exc}", file=sys.stderr)
    raise SystemExit(1)

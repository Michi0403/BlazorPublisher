using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Streaming.UseCases.Editor;

/// <summary>
/// Coordinates streaming session behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="mediaHost">Streaming media host client dependency used by the streaming session workflow to provide the corresponding application capability.</param>
public sealed class StreamingSessionService(StreamingMediaHostClient mediaHost)
{
    /// <summary>
    /// Stores the streaming media host client dependency used by <see cref="StreamingSessionService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly StreamingMediaHostClient _mediaHost = mediaHost;
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to gate state owned by <see cref="StreamingSessionService"/>.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(1, 1);
    /// <summary>
    /// Stores the internal snapshot state used by <see cref="StreamingSessionService"/> while executing its surrounding workflow.
    /// </summary>
    private StreamingSessionSnapshot _snapshot = new();
    /// <summary>
    /// Stores the cancellation source used by <see cref="StreamingSessionService"/> to stop its current background or asynchronous operation.
    /// </summary>
    private CancellationTokenSource? _eventPollCancellation;

    /// <summary>
    /// Occurs when changed changes or completes in <see cref="StreamingSessionService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action? Changed;
    /// <summary>
    /// Occurs when hotkey triggered changes or completes in <see cref="StreamingSessionService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action<MediaHostHotkeyEvent>? HotkeyTriggered;
    /// <summary>
    /// Gets the snapshot value that forms part of the streaming session state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The snapshot value exposed by <see cref="StreamingSessionService"/>.</value>
    public StreamingSessionSnapshot Snapshot => _snapshot;

    /// <summary>
    /// Performs start as part of the streaming session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the streaming session operation and used when producing its result.</param>
    /// <param name="dryRun">Value indicating whether dry run should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task StartAsync(PublicationDocument document, bool dryRun, CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_snapshot.Mode != PublicationStreamSessionMode.Idle) return;
                _snapshot = new StreamingSessionSnapshot
                {
                    Mode = dryRun ? PublicationStreamSessionMode.DryRun : PublicationStreamSessionMode.Live,
                    ProgramPageId = ResolveProgramPage(document),
                    Recording = document.Streaming.Recording.Enabled,
                    StatusText = "Preparing integrated streaming runtime…",
                    StartedUtc = DateTimeOffset.UtcNow,
                    OutputEnabled = document.Streaming.Outputs.ToDictionary(item => item.Id, item => item.Enabled)
                };
                Changed?.Invoke();
                var response = await _mediaHost.StartAsync(document, dryRun, cancellationToken).ConfigureAwait(false);
                if (response is null)
                {
                    _snapshot.Mode = PublicationStreamSessionMode.Idle;
                    _snapshot.StatusText = "The integrated streaming runtime could not prepare the session. Check FFmpeg and the configured sources.";
                    _snapshot.MediaHostConnected = false;
                    _snapshot.StartedUtc = null;
                }
                else
                {
                    _snapshot.SessionId = response.SessionId;
                    _snapshot.MediaHostConnected = true;
                    _snapshot.StatusText = DescribeActiveState();
                    StartEventPolling(response.SessionId);
                }
                Changed?.Invoke();
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionService.StartAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs stop as part of the streaming session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                StopEventPolling();
                try
                {
                    if (_snapshot.SessionId is { } sessionId)
                        await _mediaHost.StopAsync(sessionId, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _snapshot = new StreamingSessionSnapshot { StatusText = "Ready" };
                    Changed?.Invoke();
                }
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionService.StopAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Stops streaming outputs as part of the streaming session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public async Task<bool> StopStreamingOutputsAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_snapshot.Mode != PublicationStreamSessionMode.Live) return true;
                var enabledOutputs = _snapshot.OutputEnabled.Where(pair => pair.Value).Select(pair => pair.Key).ToArray();
                var succeeded = true;
                foreach (var outputId in enabledOutputs)
                {
                    if (_snapshot.SessionId is { } sessionId
                        && !await _mediaHost.SetOutputEnabledAsync(sessionId, outputId, false, cancellationToken).ConfigureAwait(false))
                    {
                        succeeded = false;
                        continue;
                    }
                    _snapshot.OutputEnabled[outputId] = false;
                }
                _snapshot.StatusText = DescribeActiveState();
                Changed?.Invoke();
                return succeeded;
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionService.StopStreamingOutputsAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs toggle output as part of the streaming session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="outputId">Identifier of the output to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task ToggleOutputAsync(Guid outputId, CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var enabled = !_snapshot.OutputEnabled.GetValueOrDefault(outputId);
                if (_snapshot.SessionId is { } sessionId
                    && !await _mediaHost.SetOutputEnabledAsync(sessionId, outputId, enabled, cancellationToken).ConfigureAwait(false)) return;
                _snapshot.OutputEnabled[outputId] = enabled;
                _snapshot.StatusText = DescribeActiveState();
                Changed?.Invoke();
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionService.ToggleOutputAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs toggle recording as part of the streaming session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async Task ToggleRecordingAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _ = await SetRecordingCoreAsync(!_snapshot.Recording, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionService.ToggleRecordingAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets recording as part of the streaming session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="enabled">Value indicating whether enabled should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public async Task<bool> SetRecordingAsync(bool enabled, CancellationToken cancellationToken = default)
    {
    try
    {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await SetRecordingCoreAsync(enabled, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionService.SetRecordingAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets recording core as part of the streaming session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="enabled">Value indicating whether enabled should apply to this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private async Task<bool> SetRecordingCoreAsync(bool enabled, CancellationToken cancellationToken)
    {
    try
    {
            if (_snapshot.Mode == PublicationStreamSessionMode.Idle) return false;
            if (_snapshot.Recording == enabled) return true;
            if (_snapshot.SessionId is { } sessionId
                && !await _mediaHost.SetRecordingAsync(sessionId, enabled, cancellationToken).ConfigureAwait(false)) return false;
            _snapshot.Recording = enabled;
            _snapshot.StatusText = DescribeActiveState();
            Changed?.Invoke();
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionService.SetRecordingCoreAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Sets program page as part of the streaming session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pageId">Identifier of the page to use for this operation.</param>
    public void SetProgramPage(Guid pageId)
    {
    try
    {
            _snapshot.ProgramPageId = pageId;
            if (_snapshot.SessionId is { } sessionId) _ = _mediaHost.SetProgramPageAsync(sessionId, pageId);
            Changed?.Invoke();
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionService.SetProgramPage failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs describe active state as part of the streaming session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The string produced by the operation.</returns>
    private string DescribeActiveState()
    {
    try
    {
            if (_snapshot.Mode == PublicationStreamSessionMode.Idle) return "Ready";
            if (_snapshot.Mode == PublicationStreamSessionMode.DryRun)
                return _snapshot.Recording ? "Dry run + recording" : "Dry run active";

            var streaming = _snapshot.OutputEnabled.Values.Any(enabled => enabled);
            return (streaming, _snapshot.Recording) switch
            {
                (true, true) => "Live + recording",
                (true, false) => "Live",
                (false, true) => "Recording only",
                _ => "Session active · provider outputs stopped"
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionService.DescribeActiveState failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Stops event polling as part of the streaming session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void StopEventPolling()
    {
    try
    {
            _eventPollCancellation?.Cancel();
            _eventPollCancellation?.Dispose();
            _eventPollCancellation = null;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionService.StopEventPolling failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Starts event polling as part of the streaming session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    private void StartEventPolling(Guid sessionId)
    {
    try
    {
            StopEventPolling();
            _eventPollCancellation = new CancellationTokenSource();
            var cancellationToken = _eventPollCancellation.Token;
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var events = await _mediaHost.ReadEventsAsync(sessionId, cancellationToken).ConfigureAwait(false);
                        foreach (var hotkeyEvent in events) HotkeyTriggered?.Invoke(hotkeyEvent);
                        await Task.Delay(300, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
                    catch
                    {
                        try { await Task.Delay(1000, cancellationToken).ConfigureAwait(false); }
                        catch (OperationCanceledException) { break; }
                    }
                }
            }, cancellationToken);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionService.StartEventPolling failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Resolves program page as part of the streaming session service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the streaming session operation and used when producing its result.</param>
    /// <returns>The GUID produced by the operation.</returns>
    private Guid? ResolveProgramPage(PublicationDocument document) {
    try
    {
        return document.Streaming.ProgramPageId is { } configured && document.Pages.Any(page => page.Id == configured)
            ? configured
            : document.Pages.FirstOrDefault()?.Id;
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingSessionService.ResolveProgramPage failed: {__serviceMethodException}");
        throw;
    }
}
}

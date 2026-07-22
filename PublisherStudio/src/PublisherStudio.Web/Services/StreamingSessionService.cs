using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed class StreamingSessionService(StreamingMediaHostClient mediaHost)
{
    private readonly StreamingMediaHostClient _mediaHost = mediaHost;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private StreamingSessionSnapshot _snapshot = new();
    private CancellationTokenSource? _eventPollCancellation;

    public event Action? Changed;
    public event Action<MediaHostHotkeyEvent>? HotkeyTriggered;
    public StreamingSessionSnapshot Snapshot => _snapshot;

    public async Task StartAsync(PublicationDocument document, bool dryRun, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
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
            var response = await _mediaHost.StartAsync(document, dryRun, cancellationToken);
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

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            StopEventPolling();
            try
            {
                if (_snapshot.SessionId is { } sessionId)
                    await _mediaHost.StopAsync(sessionId, cancellationToken);
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

    public async Task<bool> StopStreamingOutputsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_snapshot.Mode != PublicationStreamSessionMode.Live) return true;
            var enabledOutputs = _snapshot.OutputEnabled.Where(pair => pair.Value).Select(pair => pair.Key).ToArray();
            var succeeded = true;
            foreach (var outputId in enabledOutputs)
            {
                if (_snapshot.SessionId is { } sessionId
                    && !await _mediaHost.SetOutputEnabledAsync(sessionId, outputId, false, cancellationToken))
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

    public async Task ToggleOutputAsync(Guid outputId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var enabled = !_snapshot.OutputEnabled.GetValueOrDefault(outputId);
            if (_snapshot.SessionId is { } sessionId
                && !await _mediaHost.SetOutputEnabledAsync(sessionId, outputId, enabled, cancellationToken)) return;
            _snapshot.OutputEnabled[outputId] = enabled;
            _snapshot.StatusText = DescribeActiveState();
            Changed?.Invoke();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ToggleRecordingAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _ = await SetRecordingCoreAsync(!_snapshot.Recording, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> SetRecordingAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await SetRecordingCoreAsync(enabled, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> SetRecordingCoreAsync(bool enabled, CancellationToken cancellationToken)
    {
        if (_snapshot.Mode == PublicationStreamSessionMode.Idle) return false;
        if (_snapshot.Recording == enabled) return true;
        if (_snapshot.SessionId is { } sessionId
            && !await _mediaHost.SetRecordingAsync(sessionId, enabled, cancellationToken)) return false;
        _snapshot.Recording = enabled;
        _snapshot.StatusText = DescribeActiveState();
        Changed?.Invoke();
        return true;
    }

    public void SetProgramPage(Guid pageId)
    {
        _snapshot.ProgramPageId = pageId;
        if (_snapshot.SessionId is { } sessionId) _ = _mediaHost.SetProgramPageAsync(sessionId, pageId);
        Changed?.Invoke();
    }

    private string DescribeActiveState()
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

    private void StopEventPolling()
    {
        _eventPollCancellation?.Cancel();
        _eventPollCancellation?.Dispose();
        _eventPollCancellation = null;
    }

    private void StartEventPolling(Guid sessionId)
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
                    var events = await _mediaHost.ReadEventsAsync(sessionId, cancellationToken);
                    foreach (var hotkeyEvent in events) HotkeyTriggered?.Invoke(hotkeyEvent);
                    await Task.Delay(300, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
                catch
                {
                    try { await Task.Delay(1000, cancellationToken); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }, cancellationToken);
    }

    private static Guid? ResolveProgramPage(PublicationDocument document) =>
        document.Streaming.ProgramPageId is { } configured && document.Pages.Any(page => page.Id == configured)
            ? configured
            : document.Pages.FirstOrDefault()?.Id;
}

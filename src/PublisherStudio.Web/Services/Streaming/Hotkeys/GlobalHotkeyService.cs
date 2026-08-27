using System.Collections.Concurrent;

namespace PublisherStudio.Services.Streaming.Hotkeys;

/// <summary>
/// Coordinates global hotkey behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="nativeService">Windows hotkey native service dependency used by the global hotkey workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class GlobalHotkeyService(
    IGlobalHotkeyNativeService nativeService,
    ILogger<GlobalHotkeyService> logger) : IDisposable
{
    /// <summary>
    /// Defines the wm hotkey constant used by <see cref="GlobalHotkeyService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint WmHotkey = 0x0312;
    /// <summary>
    /// Defines the wm command constant used by <see cref="GlobalHotkeyService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint WmCommand = 0x8000 + 47;
    /// <summary>
    /// Defines the wm quit constant used by <see cref="GlobalHotkeyService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint WmQuit = 0x0012;
    /// <summary>
    /// Defines the mod alt constant used by <see cref="GlobalHotkeyService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint ModAlt = 0x0001;
    /// <summary>
    /// Defines the mod control constant used by <see cref="GlobalHotkeyService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint ModControl = 0x0002;
    /// <summary>
    /// Defines the mod shift constant used by <see cref="GlobalHotkeyService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint ModShift = 0x0004;
    /// <summary>
    /// Defines the mod win constant used by <see cref="GlobalHotkeyService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint ModWin = 0x0008;
    /// <summary>
    /// Defines the mod no repeat constant used by <see cref="GlobalHotkeyService"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint ModNoRepeat = 0x4000;

    /// <summary>
    /// Stores the internal commands state used by <see cref="GlobalHotkeyService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly ConcurrentQueue<Action> _commands = new();
    /// <summary>
    /// Stores the in-memory events collection maintained internally by <see cref="GlobalHotkeyService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<MediaHostHotkeyEvent>> _events = new();
    /// <summary>
    /// Stores the in-memory registered collection maintained internally by <see cref="GlobalHotkeyService"/> for its current workflow state.
    /// </summary>
    private readonly Dictionary<int, RegisteredHotkey> _registered = [];
    /// <summary>
    /// Stores the internal started state used by <see cref="GlobalHotkeyService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly ManualResetEventSlim _started = new(false);
    /// <summary>
    /// Stores the internal thread state used by <see cref="GlobalHotkeyService"/> while executing its surrounding workflow.
    /// </summary>
    private Thread? _thread;
    /// <summary>
    /// Stores the internal thread identifier state used by <see cref="GlobalHotkeyService"/> while executing its surrounding workflow.
    /// </summary>
    private uint _threadId;
    /// <summary>
    /// Stores the internal next native identifier state used by <see cref="GlobalHotkeyService"/> while executing its surrounding workflow.
    /// </summary>
    private int _nextNativeId = 100;

    /// <summary>
    /// Performs start as part of the global hotkey service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
    try
    {
            if (!nativeService.IsAvailable) return Task.CompletedTask;
            _thread = new Thread(MessageLoop) { IsBackground = true, Name = "PublisherStudio global hotkeys" };
            _thread.Start();
            _started.Wait(cancellationToken);
            return Task.CompletedTask;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(StartAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(StartAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs stop as part of the global hotkey service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
    try
    {
            if (_thread is null) return Task.CompletedTask;
            nativeService.TryPostThreadMessage(_threadId, WmQuit, UIntPtr.Zero, IntPtr.Zero);
            _thread.Join(TimeSpan.FromSeconds(2));
            return Task.CompletedTask;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(StopAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(StopAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs configure as part of the global hotkey service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="hotkeys">Media hotkey dependency used by the global hotkey workflow to provide the corresponding application capability.</param>
    public void Configure(Guid sessionId, IEnumerable<MediaHotkey> hotkeys)
    {
    try
    {
            if (!nativeService.IsAvailable) return;
            Enqueue(() =>
            {
                RemoveCore(sessionId);
                foreach (var hotkey in hotkeys.Where(item => !string.IsNullOrWhiteSpace(item.Gesture) && !string.IsNullOrWhiteSpace(item.Command)))
                {
                    if (!TryParseGesture(hotkey.Gesture, out var modifiers, out var virtualKey)) continue;
                    var nativeId = Interlocked.Increment(ref _nextNativeId);
                    if (!nativeService.TryRegisterHotKey(IntPtr.Zero, nativeId, modifiers | ModNoRepeat, virtualKey)) continue;
                    _registered[nativeId] = new RegisteredHotkey(sessionId, hotkey.Command, hotkey.TargetId);
                    _events.TryAdd(sessionId, new ConcurrentQueue<MediaHostHotkeyEvent>());
                }
            });
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(Configure)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(Configure)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs remove as part of the global hotkey service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    public void Remove(Guid sessionId)
    {
    try
    {
            if (!nativeService.IsAvailable) return;
            Enqueue(() => RemoveCore(sessionId));
            _events.TryRemove(sessionId, out _);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(Remove)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(Remove)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs drain as part of the global hotkey service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<MediaHostHotkeyEvent> Drain(Guid sessionId)
    {
    try
    {
            if (!_events.TryGetValue(sessionId, out var queue)) return [];
            var result = new List<MediaHostHotkeyEvent>();
            while (result.Count < 100 && queue.TryDequeue(out var item)) result.Add(item);
            return result;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(Drain)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(Drain)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs enqueue as part of the global hotkey service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="command">Command value supplied to the global hotkey operation and used when producing its result.</param>
    private void Enqueue(Action command)
    {
    try
    {
            _commands.Enqueue(command);
            if (_threadId != 0) nativeService.TryPostThreadMessage(_threadId, WmCommand, UIntPtr.Zero, IntPtr.Zero);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(Enqueue)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(Enqueue)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs message loop as part of the global hotkey service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void MessageLoop()
    {
    try
    {
            if (!nativeService.TryInitializeMessageQueue(out _threadId))
            {
                logger.LogWarning($"Windows global-hotkey message loop could not initialize.");
                _started.Set();
                return;
            }
            _started.Set();
            while (nativeService.ReadMessage(out var message) > 0)
            {
                if (message.Message == WmCommand)
                {
                    while (_commands.TryDequeue(out var command)) command();
                    continue;
                }
                if (message.Message != WmHotkey) continue;
                var nativeId = unchecked((int)message.WordParameter.ToUInt64());
                if (!_registered.TryGetValue(nativeId, out var hotkey)) continue;
                var queue = _events.GetOrAdd(hotkey.SessionId, _ => new ConcurrentQueue<MediaHostHotkeyEvent>());
                queue.Enqueue(new MediaHostHotkeyEvent(hotkey.Command, hotkey.TargetId, DateTimeOffset.UtcNow));
            }
            foreach (var nativeId in _registered.Keys.ToArray()) nativeService.TryUnregisterHotKey(IntPtr.Zero, nativeId);
            _registered.Clear();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(MessageLoop)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(MessageLoop)} failed.");
        throw;
    }
}

    /// <summary>
    /// Removes core as part of the global hotkey service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    private void RemoveCore(Guid sessionId)
    {
    try
    {
            foreach (var pair in _registered.Where(pair => pair.Value.SessionId == sessionId).ToArray())
            {
                nativeService.TryUnregisterHotKey(IntPtr.Zero, pair.Key);
                _registered.Remove(pair.Key);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(RemoveCore)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(RemoveCore)} failed.");
        throw;
    }
}

    /// <summary>
    /// Attempts to parse gesture as part of the global hotkey service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="gesture">Gesture value supplied to the global hotkey operation and used when producing its result.</param>
    /// <param name="modifiers">Modifiers value supplied to the global hotkey operation and used when producing its result.</param>
    /// <param name="virtualKey">Virtual key value supplied to the global hotkey operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryParseGesture(string gesture, out uint modifiers, out uint virtualKey)
    {
    try
    {
            modifiers = 0;
            virtualKey = 0;
            var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0) return false;
            foreach (var part in parts[..^1])
            {
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase)) modifiers |= ModControl;
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) modifiers |= ModAlt;
                else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) modifiers |= ModShift;
                else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) || part.Equals("Meta", StringComparison.OrdinalIgnoreCase)) modifiers |= ModWin;
            }
            var key = parts[^1];
            if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
            {
                virtualKey = char.ToUpperInvariant(key[0]);
                return true;
            }
            if (key.StartsWith("F", StringComparison.OrdinalIgnoreCase) && int.TryParse(key[1..], out var functionKey) && functionKey is >= 1 and <= 24)
            {
                virtualKey = (uint)(0x70 + functionKey - 1);
                return true;
            }
            virtualKey = key.ToUpperInvariant() switch
            {
                "PAGEUP" => 0x21,
                "PAGEDOWN" => 0x22,
                "END" => 0x23,
                "HOME" => 0x24,
                "ARROWLEFT" or "LEFT" => 0x25,
                "ARROWUP" or "UP" => 0x26,
                "ARROWRIGHT" or "RIGHT" => 0x27,
                "ARROWDOWN" or "DOWN" => 0x28,
                "INSERT" => 0x2D,
                "DELETE" or "DEL" => 0x2E,
                "SPACE" => 0x20,
                "ESCAPE" or "ESC" => 0x1B,
                _ => 0
            };
            return virtualKey != 0;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(TryParseGesture)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(TryParseGesture)} failed.");
        throw;
    }
}

    /// <summary>
    /// Releases resources owned by <see cref="GlobalHotkeyService"/> and leaves the global hotkey workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
    try
    {
            try { StopAsync(CancellationToken.None).GetAwaiter().GetResult(); } catch { }
            _started.Dispose();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(Dispose)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(GlobalHotkeyService)}.{nameof(Dispose)} failed.");
        throw;
    }
}

    /// <summary>
    /// Represents a registered hotkey helper type nested within <see cref="GlobalHotkeyService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="SessionId">Identifier of the session to use for this operation.</param>
    /// <param name="Command">Command value supplied to the global hotkey operation and used when producing its result.</param>
    /// <param name="TargetId">Identifier of the target to use for this operation.</param>
    private sealed record RegisteredHotkey(Guid SessionId, string Command, Guid? TargetId);

}

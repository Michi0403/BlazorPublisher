using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace PublisherStudio.HostedServices.Streaming;

public sealed class GlobalHotkeyService : IHostedService, IDisposable
{
    private const uint WmHotkey = 0x0312;
    private const uint WmCommand = 0x8000 + 47;
    private const uint WmQuit = 0x0012;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;

    private readonly ConcurrentQueue<Action> _commands = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<MediaHostHotkeyEvent>> _events = new();
    private readonly Dictionary<int, RegisteredHotkey> _registered = [];
    private readonly ManualResetEventSlim _started = new(false);
    private Thread? _thread;
    private uint _threadId;
    private int _nextNativeId = 100;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) return Task.CompletedTask;
        _thread = new Thread(MessageLoop) { IsBackground = true, Name = "PublisherStudio global hotkeys" };
        _thread.Start();
        _started.Wait(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_thread is null) return Task.CompletedTask;
        PostThreadMessage(_threadId, WmQuit, UIntPtr.Zero, IntPtr.Zero);
        _thread.Join(TimeSpan.FromSeconds(2));
        return Task.CompletedTask;
    }

    public void Configure(Guid sessionId, IEnumerable<MediaHotkey> hotkeys)
    {
        if (!OperatingSystem.IsWindows()) return;
        Enqueue(() =>
        {
            RemoveCore(sessionId);
            foreach (var hotkey in hotkeys.Where(item => !string.IsNullOrWhiteSpace(item.Gesture) && !string.IsNullOrWhiteSpace(item.Command)))
            {
                if (!TryParseGesture(hotkey.Gesture, out var modifiers, out var virtualKey)) continue;
                var nativeId = Interlocked.Increment(ref _nextNativeId);
                if (!RegisterHotKey(IntPtr.Zero, nativeId, modifiers | ModNoRepeat, virtualKey)) continue;
                _registered[nativeId] = new RegisteredHotkey(sessionId, hotkey.Command, hotkey.TargetId);
                _events.TryAdd(sessionId, new ConcurrentQueue<MediaHostHotkeyEvent>());
            }
        });
    }

    public void Remove(Guid sessionId)
    {
        if (!OperatingSystem.IsWindows()) return;
        Enqueue(() => RemoveCore(sessionId));
        _events.TryRemove(sessionId, out _);
    }

    public IReadOnlyList<MediaHostHotkeyEvent> Drain(Guid sessionId)
    {
        if (!_events.TryGetValue(sessionId, out var queue)) return [];
        var result = new List<MediaHostHotkeyEvent>();
        while (result.Count < 100 && queue.TryDequeue(out var item)) result.Add(item);
        return result;
    }

    private void Enqueue(Action command)
    {
        _commands.Enqueue(command);
        if (_threadId != 0) PostThreadMessage(_threadId, WmCommand, UIntPtr.Zero, IntPtr.Zero);
    }

    private void MessageLoop()
    {
        _threadId = GetCurrentThreadId();
        PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        _started.Set();
        while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
        {
            if (message.Message == WmCommand)
            {
                while (_commands.TryDequeue(out var command)) command();
                continue;
            }
            if (message.Message != WmHotkey) continue;
            var nativeId = unchecked((int)message.WParam.ToUInt64());
            if (!_registered.TryGetValue(nativeId, out var hotkey)) continue;
            var queue = _events.GetOrAdd(hotkey.SessionId, _ => new ConcurrentQueue<MediaHostHotkeyEvent>());
            queue.Enqueue(new MediaHostHotkeyEvent(hotkey.Command, hotkey.TargetId, DateTimeOffset.UtcNow));
        }
        foreach (var nativeId in _registered.Keys.ToArray()) UnregisterHotKey(IntPtr.Zero, nativeId);
        _registered.Clear();
    }

    private void RemoveCore(Guid sessionId)
    {
        foreach (var pair in _registered.Where(pair => pair.Value.SessionId == sessionId).ToArray())
        {
            UnregisterHotKey(IntPtr.Zero, pair.Key);
            _registered.Remove(pair.Key);
        }
    }

    private static bool TryParseGesture(string gesture, out uint modifiers, out uint virtualKey)
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

    public void Dispose()
    {
        try { StopAsync(CancellationToken.None).GetAwaiter().GetResult(); } catch { }
        _started.Dispose();
    }

    private sealed record RegisteredHotkey(Guid SessionId, string Command, Guid? TargetId);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr HWnd;
        public uint Message;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public NativePoint Point;
        public uint Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out NativeMessage message, IntPtr hWnd, uint minFilter, uint maxFilter);

    [DllImport("user32.dll")]
    private static extern bool PeekMessage(out NativeMessage message, IntPtr hWnd, uint minFilter, uint maxFilter, uint removeMessage);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint threadId, uint message, UIntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}


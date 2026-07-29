using System.Runtime.InteropServices;

namespace PublisherStudio.Services.Streaming.Hotkeys;

public interface IWindowsHotkeyNativeService
{
    bool IsAvailable { get; }
    bool TryInitializeMessageQueue(out uint threadId);
    bool TryRegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);
    bool TryUnregisterHotKey(IntPtr windowHandle, int id);
    int ReadMessage(out WindowsHotkeyNativeMessage message);
    bool TryPostThreadMessage(uint threadId, uint message, UIntPtr wordParameter, IntPtr longParameter);
}

[StructLayout(LayoutKind.Sequential)]
public struct WindowsHotkeyNativeMessage
{
    public IntPtr WindowHandle;
    public uint Message;
    public UIntPtr WordParameter;
    public IntPtr LongParameter;
    public uint Time;
    public WindowsHotkeyNativePoint Point;
    public uint Private;
}

[StructLayout(LayoutKind.Sequential)]
public struct WindowsHotkeyNativePoint
{
    public int X;
    public int Y;
}

/// <summary>
/// Instance-owned user32/kernel32 binding used by the global-hotkey runtime.
/// Native exports are resolved once per application lifetime so application code
/// does not require static P/Invoke declarations.
/// </summary>
public sealed class WindowsHotkeyNativeService : IWindowsHotkeyNativeService, IDisposable
{
    private readonly ILogger<WindowsHotkeyNativeService> logger;
    private IntPtr user32Library;
    private IntPtr kernel32Library;
    private RegisterHotKeyDelegate? registerHotKey;
    private UnregisterHotKeyDelegate? unregisterHotKey;
    private GetMessageDelegate? getMessage;
    private PeekMessageDelegate? peekMessage;
    private PostThreadMessageDelegate? postThreadMessage;
    private GetCurrentThreadIdDelegate? getCurrentThreadId;
    private bool disposed;

    public WindowsHotkeyNativeService(ILogger<WindowsHotkeyNativeService> logger)
    {
        this.logger = logger;
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                logger.LogDebug($"Windows global-hotkey native bindings are disabled on this operating system.");
                return;
            }

            user32Library = NativeLibrary.Load("user32.dll");
            kernel32Library = NativeLibrary.Load("kernel32.dll");
            registerHotKey = LoadDelegate<RegisterHotKeyDelegate>(user32Library, "RegisterHotKey");
            unregisterHotKey = LoadDelegate<UnregisterHotKeyDelegate>(user32Library, "UnregisterHotKey");
            getMessage = LoadDelegate<GetMessageDelegate>(user32Library, "GetMessageW");
            peekMessage = LoadDelegate<PeekMessageDelegate>(user32Library, "PeekMessageW");
            postThreadMessage = LoadDelegate<PostThreadMessageDelegate>(user32Library, "PostThreadMessageW");
            getCurrentThreadId = LoadDelegate<GetCurrentThreadIdDelegate>(kernel32Library, "GetCurrentThreadId");
            IsAvailable = true;
            logger.LogInformation($"Initialized instance-owned Windows global-hotkey native bindings.");
        }
        catch (Exception exception)
        {
            ReleaseLibraries();
            logger.LogError(exception, $"Could not initialize Windows global-hotkey native bindings: {exception.Message}");
        }
    }

    public bool IsAvailable { get; private set; }

    public bool TryInitializeMessageQueue(out uint threadId)
    {
        try
        {
            threadId = 0;
            if (!IsAvailable || getCurrentThreadId is null || peekMessage is null)
                return false;

            threadId = getCurrentThreadId();
            var initialized = peekMessage(out _, IntPtr.Zero, 0, 0, 0);
            logger.LogDebug($"Initialized the Windows hotkey message queue for thread {threadId}; initial message availability was {initialized}.");
            return threadId != 0;
        }
        catch (Exception exception)
        {
            threadId = 0;
            logger.LogError(exception, $"Could not initialize the Windows hotkey message queue: {exception.Message}");
            return false;
        }
    }

    public bool TryRegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey)
    {
        try
        {
            if (!IsAvailable || registerHotKey is null)
                return false;
            var registered = registerHotKey(windowHandle, id, modifiers, virtualKey);
            if (!registered)
                logger.LogWarning($"Windows rejected global-hotkey registration {id} with native error {Marshal.GetLastWin32Error()}.");
            return registered;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not register Windows global hotkey {id}: {exception.Message}");
            return false;
        }
    }

    public bool TryUnregisterHotKey(IntPtr windowHandle, int id)
    {
        try
        {
            if (!IsAvailable || unregisterHotKey is null)
                return false;
            var unregistered = unregisterHotKey(windowHandle, id);
            if (!unregistered)
                logger.LogDebug($"Windows global-hotkey registration {id} was already unavailable or could not be removed; native error {Marshal.GetLastWin32Error()}.");
            return unregistered;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not unregister Windows global hotkey {id}: {exception.Message}");
            return false;
        }
    }

    public int ReadMessage(out WindowsHotkeyNativeMessage message)
    {
        try
        {
            message = default;
            if (!IsAvailable || getMessage is null)
                return 0;
            var result = getMessage(out message, IntPtr.Zero, 0, 0);
            if (result < 0)
                logger.LogError($"Windows hotkey message retrieval failed with native error {Marshal.GetLastWin32Error()}.");
            return result;
        }
        catch (Exception exception)
        {
            message = default;
            logger.LogError(exception, $"Could not read the Windows hotkey message queue: {exception.Message}");
            return -1;
        }
    }

    public bool TryPostThreadMessage(uint threadId, uint message, UIntPtr wordParameter, IntPtr longParameter)
    {
        try
        {
            if (!IsAvailable || postThreadMessage is null || threadId == 0)
                return false;
            var posted = postThreadMessage(threadId, message, wordParameter, longParameter);
            if (!posted)
                logger.LogDebug($"Windows thread message {message} could not be posted to thread {threadId}; native error {Marshal.GetLastWin32Error()}.");
            return posted;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not post Windows thread message {message} to thread {threadId}: {exception.Message}");
            return false;
        }
    }

    private TDelegate LoadDelegate<TDelegate>(IntPtr library, string exportName)
        where TDelegate : Delegate
    {
        try
        {
            var export = NativeLibrary.GetExport(library, exportName);
            var binding = Marshal.GetDelegateForFunctionPointer<TDelegate>(export);
            logger.LogTrace($"Resolved Windows native export {exportName} for {typeof(TDelegate).Name}.");
            return binding;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve Windows native export {exportName}: {exception.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            if (disposed)
                return;
            disposed = true;
            IsAvailable = false;
            registerHotKey = null;
            unregisterHotKey = null;
            getMessage = null;
            peekMessage = null;
            postThreadMessage = null;
            getCurrentThreadId = null;
            ReleaseLibraries();
            logger.LogDebug($"Released Windows global-hotkey native bindings.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not release Windows global-hotkey native bindings cleanly: {exception.Message}");
        }
    }

    private void ReleaseLibraries()
    {
        try
        {
            if (user32Library != IntPtr.Zero)
            {
                NativeLibrary.Free(user32Library);
                user32Library = IntPtr.Zero;
            }
            if (kernel32Library != IntPtr.Zero)
            {
                NativeLibrary.Free(kernel32Library);
                kernel32Library = IntPtr.Zero;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not release one or more Windows hotkey native libraries: {exception.Message}");
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool RegisterHotKeyDelegate(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool UnregisterHotKeyDelegate(IntPtr windowHandle, int id);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    private delegate int GetMessageDelegate(out WindowsHotkeyNativeMessage message, IntPtr windowHandle, uint minimumFilter, uint maximumFilter);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool PeekMessageDelegate(out WindowsHotkeyNativeMessage message, IntPtr windowHandle, uint minimumFilter, uint maximumFilter, uint removeMessage);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool PostThreadMessageDelegate(uint threadId, uint message, UIntPtr wordParameter, IntPtr longParameter);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate uint GetCurrentThreadIdDelegate();
}

using System.Runtime.InteropServices;

namespace PublisherStudio.Services.Streaming.Hotkeys;

/// <summary>
/// Defines the contract for windows hotkey native behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IGlobalHotkeyNativeService
{
    /// <summary>
    /// Gets a value indicating whether available applies to the windows hotkey native state.
    /// </summary>
    /// <value>The is available value exposed by <see cref="IGlobalHotkeyNativeService"/>.</value>
    bool IsAvailable { get; }
    /// <summary>
    /// Attempts to initialize message queue as part of the windows hotkey native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="threadId">Identifier of the thread to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool TryInitializeMessageQueue(out uint threadId);
    /// <summary>
    /// Attempts to register hot key as part of the windows hotkey native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="windowHandle">Int ptr dependency used by the windows hotkey native workflow to provide the corresponding application capability.</param>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="modifiers">Modifiers value supplied to the windows hotkey native operation and used when producing its result.</param>
    /// <param name="virtualKey">Virtual key value supplied to the windows hotkey native operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool TryRegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);
    /// <summary>
    /// Attempts to unregister hot key as part of the windows hotkey native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="windowHandle">Int ptr dependency used by the windows hotkey native workflow to provide the corresponding application capability.</param>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool TryUnregisterHotKey(IntPtr windowHandle, int id);
    /// <summary>
    /// Reads message as part of the windows hotkey native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the windows hotkey native operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    int ReadMessage(out GlobalHotkeyNativeMessage message);
    /// <summary>
    /// Attempts to post thread message as part of the windows hotkey native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="threadId">Identifier of the thread to use for this operation.</param>
    /// <param name="message">Message value supplied to the windows hotkey native operation and used when producing its result.</param>
    /// <param name="wordParameter">Word parameter value supplied to the windows hotkey native operation and used when producing its result.</param>
    /// <param name="longParameter">Int ptr dependency used by the windows hotkey native workflow to provide the corresponding application capability.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool TryPostThreadMessage(uint threadId, uint message, UIntPtr wordParameter, IntPtr longParameter);
}

/// <summary>
/// Represents a windows hotkey native message application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GlobalHotkeyNativeMessage
{
    /// <summary>
    /// Stores the internal window handle state used by <see cref="GlobalHotkeyNativeMessage"/> while executing its surrounding workflow.
    /// </summary>
    public IntPtr WindowHandle;
    /// <summary>
    /// Stores the internal message state used by <see cref="GlobalHotkeyNativeMessage"/> while executing its surrounding workflow.
    /// </summary>
    public uint Message;
    /// <summary>
    /// Stores the internal word parameter state used by <see cref="GlobalHotkeyNativeMessage"/> while executing its surrounding workflow.
    /// </summary>
    public UIntPtr WordParameter;
    /// <summary>
    /// Stores the internal long parameter state used by <see cref="GlobalHotkeyNativeMessage"/> while executing its surrounding workflow.
    /// </summary>
    public IntPtr LongParameter;
    /// <summary>
    /// Stores the internal time state used by <see cref="GlobalHotkeyNativeMessage"/> while executing its surrounding workflow.
    /// </summary>
    public uint Time;
    /// <summary>
    /// Stores the internal point state used by <see cref="GlobalHotkeyNativeMessage"/> while executing its surrounding workflow.
    /// </summary>
    public GlobalHotkeyNativePoint Point;
    /// <summary>
    /// Stores the internal private state used by <see cref="GlobalHotkeyNativeMessage"/> while executing its surrounding workflow.
    /// </summary>
    public uint Private;
}

/// <summary>
/// Represents a windows hotkey native point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GlobalHotkeyNativePoint
{
    /// <summary>
    /// Stores the internal x state used by <see cref="GlobalHotkeyNativePoint"/> while executing its surrounding workflow.
    /// </summary>
    public int X;
    /// <summary>
    /// Stores the internal y state used by <see cref="GlobalHotkeyNativePoint"/> while executing its surrounding workflow.
    /// </summary>
    public int Y;
}

/// <summary>
/// Instance-owned user32/kernel32 binding used by the global-hotkey runtime.
/// Native exports are resolved once per application lifetime so application code
/// does not require static P/Invoke declarations.
/// </summary>
public sealed class WindowsHotkeyNativeService : IGlobalHotkeyNativeService, IDisposable
{
    /// <summary>
    /// Stores the logger used by <see cref="WindowsHotkeyNativeService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<WindowsHotkeyNativeService> logger;
    /// <summary>
    /// Stores the internal user32 library state used by <see cref="WindowsHotkeyNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private IntPtr user32Library;
    /// <summary>
    /// Stores the internal kernel32 library state used by <see cref="WindowsHotkeyNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private IntPtr kernel32Library;
    /// <summary>
    /// Stores the internal register hot key state used by <see cref="WindowsHotkeyNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private RegisterHotKeyDelegate? registerHotKey;
    /// <summary>
    /// Stores the internal unregister hot key state used by <see cref="WindowsHotkeyNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private UnregisterHotKeyDelegate? unregisterHotKey;
    /// <summary>
    /// Stores the internal get message state used by <see cref="WindowsHotkeyNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private GetMessageDelegate? getMessage;
    /// <summary>
    /// Stores the internal peek message state used by <see cref="WindowsHotkeyNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private PeekMessageDelegate? peekMessage;
    /// <summary>
    /// Stores the internal post thread message state used by <see cref="WindowsHotkeyNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private PostThreadMessageDelegate? postThreadMessage;
    /// <summary>
    /// Stores the internal get current thread identifier state used by <see cref="WindowsHotkeyNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private GetCurrentThreadIdDelegate? getCurrentThreadId;
    /// <summary>
    /// Stores the internal disposed state used by <see cref="WindowsHotkeyNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private bool disposed;

    /// <summary>
    /// Initializes a new <see cref="WindowsHotkeyNativeService"/> instance and captures the dependencies or initial state required by its windows hotkey native workflow.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
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

    /// <summary>
    /// Gets or sets a value indicating whether available applies to the windows hotkey native state.
    /// </summary>
    /// <value>The is available value exposed by <see cref="WindowsHotkeyNativeService"/>.</value>
    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Attempts to initialize message queue as part of the windows hotkey native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="threadId">Identifier of the thread to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Attempts to register hot key as part of the windows hotkey native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="windowHandle">Int ptr dependency used by the windows hotkey native workflow to provide the corresponding application capability.</param>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="modifiers">Modifiers value supplied to the windows hotkey native operation and used when producing its result.</param>
    /// <param name="virtualKey">Virtual key value supplied to the windows hotkey native operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Attempts to unregister hot key as part of the windows hotkey native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="windowHandle">Int ptr dependency used by the windows hotkey native workflow to provide the corresponding application capability.</param>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Reads message as part of the windows hotkey native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the windows hotkey native operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    public int ReadMessage(out GlobalHotkeyNativeMessage message)
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

    /// <summary>
    /// Attempts to post thread message as part of the windows hotkey native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="threadId">Identifier of the thread to use for this operation.</param>
    /// <param name="message">Message value supplied to the windows hotkey native operation and used when producing its result.</param>
    /// <param name="wordParameter">Word parameter value supplied to the windows hotkey native operation and used when producing its result.</param>
    /// <param name="longParameter">Int ptr dependency used by the windows hotkey native workflow to provide the corresponding application capability.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Loads delegate as part of the windows hotkey native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="TDelegate">Type used for t delegate values handled by <see cref="WindowsHotkeyNativeService"/>.</typeparam>
    /// <param name="library">Int ptr dependency used by the windows hotkey native workflow to provide the corresponding application capability.</param>
    /// <param name="exportName">Export name value supplied to the windows hotkey native operation and used when producing its result.</param>
    /// <returns>The t delegate produced by the operation.</returns>
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

    /// <summary>
    /// Releases resources owned by <see cref="WindowsHotkeyNativeService"/> and leaves the windows hotkey native workflow in a safely disposed state.
    /// </summary>
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

    /// <summary>
    /// Performs release libraries as part of the windows hotkey native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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

    /// <summary>
    /// Defines the callback signature used to report or process bool information between collaborating components.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool RegisterHotKeyDelegate(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);

    /// <summary>
    /// Defines the callback signature used to report or process bool information between collaborating components.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool UnregisterHotKeyDelegate(IntPtr windowHandle, int id);

    /// <summary>
    /// Defines the callback signature used to report or process int information between collaborating components.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    private delegate int GetMessageDelegate(out GlobalHotkeyNativeMessage message, IntPtr windowHandle, uint minimumFilter, uint maximumFilter);

    /// <summary>
    /// Defines the callback signature used to report or process bool information between collaborating components.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool PeekMessageDelegate(out GlobalHotkeyNativeMessage message, IntPtr windowHandle, uint minimumFilter, uint maximumFilter, uint removeMessage);

    /// <summary>
    /// Defines the callback signature used to report or process bool information between collaborating components.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool PostThreadMessageDelegate(uint threadId, uint message, UIntPtr wordParameter, IntPtr longParameter);

    /// <summary>
    /// Defines the callback signature used to report or process uint information between collaborating components.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate uint GetCurrentThreadIdDelegate();
}

using System.Runtime.InteropServices;

namespace PublisherStudio.Services.Streaming.Capture;

/// <summary>
/// Defines the contract for windows process loopback native behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IProcessLoopbackNativeService
{
    /// <summary>
    /// Gets a value indicating whether available applies to the windows process loopback native state.
    /// </summary>
    /// <value>The is available value exposed by <see cref="IProcessLoopbackNativeService"/>.</value>
    bool IsAvailable { get; }
    /// <summary>
    /// Performs initialize multithreaded apartment as part of the windows process loopback native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The int produced by the operation.</returns>
    int InitializeMultithreadedApartment();
    /// <summary>
    /// Performs uninitialize apartment as part of the windows process loopback native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    void UninitializeApartment();
    /// <summary>
    /// Performs activate audio interface as part of the windows process loopback native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="deviceInterfacePath">Device interface path value supplied to the windows process loopback native operation and used when producing its result.</param>
    /// <param name="interfaceId">Identifier of the interface to use for this operation.</param>
    /// <param name="activationParameters">Int ptr dependency used by the windows process loopback native workflow to provide the corresponding application capability.</param>
    /// <param name="completionHandler">Completion handler value supplied to the windows process loopback native operation and used when producing its result.</param>
    /// <param name="activationOperation">Activation operation value supplied to the windows process loopback native operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    int ActivateAudioInterface(
        string deviceInterfacePath,
        ref Guid interfaceId,
        IntPtr activationParameters,
        object completionHandler,
        out object activationOperation);
}

/// <summary>
/// Instance-owned binding for the Windows process-loopback activation APIs.
/// It replaces static DllImport declarations while preserving the COM activation flow.
/// </summary>
public sealed class WindowsProcessLoopbackNativeService : IProcessLoopbackNativeService, IDisposable
{
    /// <summary>
    /// Stores the logger used by <see cref="WindowsProcessLoopbackNativeService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<WindowsProcessLoopbackNativeService> logger;
    /// <summary>
    /// Stores the internal multimedia device library state used by <see cref="WindowsProcessLoopbackNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private IntPtr multimediaDeviceLibrary;
    /// <summary>
    /// Stores the internal ole library state used by <see cref="WindowsProcessLoopbackNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private IntPtr oleLibrary;
    /// <summary>
    /// Stores the internal activate audio interface state used by <see cref="WindowsProcessLoopbackNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private ActivateAudioInterfaceDelegate? activateAudioInterface;
    /// <summary>
    /// Stores the internal initialize apartment state used by <see cref="WindowsProcessLoopbackNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private CoInitializeExDelegate? initializeApartment;
    /// <summary>
    /// Stores the internal uninitialize apartment state used by <see cref="WindowsProcessLoopbackNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private CoUninitializeDelegate? uninitializeApartment;
    /// <summary>
    /// Stores the internal disposed state used by <see cref="WindowsProcessLoopbackNativeService"/> while executing its surrounding workflow.
    /// </summary>
    private bool disposed;

    /// <summary>
    /// Initializes a new <see cref="WindowsProcessLoopbackNativeService"/> instance and captures the dependencies or initial state required by its windows process loopback native workflow.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public WindowsProcessLoopbackNativeService(ILogger<WindowsProcessLoopbackNativeService> logger)
    {
        this.logger = logger;
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                logger.LogDebug($"Windows process-loopback native bindings are disabled on this operating system.");
                return;
            }

            multimediaDeviceLibrary = NativeLibrary.Load("Mmdevapi.dll");
            oleLibrary = NativeLibrary.Load("ole32.dll");
            activateAudioInterface = LoadDelegate<ActivateAudioInterfaceDelegate>(multimediaDeviceLibrary, "ActivateAudioInterfaceAsync");
            initializeApartment = LoadDelegate<CoInitializeExDelegate>(oleLibrary, "CoInitializeEx");
            uninitializeApartment = LoadDelegate<CoUninitializeDelegate>(oleLibrary, "CoUninitialize");
            IsAvailable = true;
            logger.LogInformation($"Initialized instance-owned Windows process-loopback native bindings.");
        }
        catch (Exception exception)
        {
            ReleaseLibraries();
            logger.LogError(exception, $"Could not initialize Windows process-loopback native bindings: {exception.Message}");
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether available applies to the windows process loopback native state.
    /// </summary>
    /// <value>The is available value exposed by <see cref="WindowsProcessLoopbackNativeService"/>.</value>
    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Performs initialize multithreaded apartment as part of the windows process loopback native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The int produced by the operation.</returns>
    public int InitializeMultithreadedApartment()
    {
        try
        {
            if (!IsAvailable || initializeApartment is null)
                throw new PlatformNotSupportedException("Windows process-loopback native bindings are unavailable.");
            var result = initializeApartment(IntPtr.Zero, 0);
            logger.LogTrace($"Initialized a Windows COM multithreaded apartment with result {result}.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not initialize the Windows COM apartment for process loopback: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs uninitialize apartment as part of the windows process loopback native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    public void UninitializeApartment()
    {
        try
        {
            if (!IsAvailable || uninitializeApartment is null)
                return;
            uninitializeApartment();
            logger.LogTrace($"Uninitialized the Windows COM apartment used by process loopback.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not uninitialize the Windows COM apartment cleanly: {exception.Message}");
        }
    }

    /// <summary>
    /// Performs activate audio interface as part of the windows process loopback native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="deviceInterfacePath">Device interface path value supplied to the windows process loopback native operation and used when producing its result.</param>
    /// <param name="interfaceId">Identifier of the interface to use for this operation.</param>
    /// <param name="activationParameters">Int ptr dependency used by the windows process loopback native workflow to provide the corresponding application capability.</param>
    /// <param name="completionHandler">Completion handler value supplied to the windows process loopback native operation and used when producing its result.</param>
    /// <param name="activationOperation">Activation operation value supplied to the windows process loopback native operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    public int ActivateAudioInterface(
        string deviceInterfacePath,
        ref Guid interfaceId,
        IntPtr activationParameters,
        object completionHandler,
        out object activationOperation)
    {
        try
        {
            if (!IsAvailable || activateAudioInterface is null)
                throw new PlatformNotSupportedException("Windows process-loopback native bindings are unavailable.");
            var result = activateAudioInterface(
                deviceInterfacePath,
                ref interfaceId,
                activationParameters,
                completionHandler,
                out activationOperation);
            logger.LogTrace($"Requested Windows process-loopback audio activation with result {result}.");
            return result;
        }
        catch (Exception exception)
        {
            activationOperation = null!;
            logger.LogError(exception, $"Could not activate the Windows process-loopback audio interface: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Loads delegate as part of the windows process loopback native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <typeparam name="TDelegate">Type used for t delegate values handled by <see cref="WindowsProcessLoopbackNativeService"/>.</typeparam>
    /// <param name="library">Int ptr dependency used by the windows process loopback native workflow to provide the corresponding application capability.</param>
    /// <param name="exportName">Export name value supplied to the windows process loopback native operation and used when producing its result.</param>
    /// <returns>The t delegate produced by the operation.</returns>
    private TDelegate LoadDelegate<TDelegate>(IntPtr library, string exportName)
        where TDelegate : Delegate
    {
        try
        {
            var export = NativeLibrary.GetExport(library, exportName);
            var binding = Marshal.GetDelegateForFunctionPointer<TDelegate>(export);
            logger.LogTrace($"Resolved Windows process-loopback native export {exportName} for {typeof(TDelegate).Name}.");
            return binding;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve Windows process-loopback native export {exportName}: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Releases resources owned by <see cref="WindowsProcessLoopbackNativeService"/> and leaves the windows process loopback native workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (disposed)
                return;
            disposed = true;
            IsAvailable = false;
            activateAudioInterface = null;
            initializeApartment = null;
            uninitializeApartment = null;
            ReleaseLibraries();
            logger.LogDebug($"Released Windows process-loopback native bindings.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not release Windows process-loopback native bindings cleanly: {exception.Message}");
        }
    }

    /// <summary>
    /// Performs release libraries as part of the windows process loopback native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void ReleaseLibraries()
    {
        try
        {
            if (multimediaDeviceLibrary != IntPtr.Zero)
            {
                NativeLibrary.Free(multimediaDeviceLibrary);
                multimediaDeviceLibrary = IntPtr.Zero;
            }
            if (oleLibrary != IntPtr.Zero)
            {
                NativeLibrary.Free(oleLibrary);
                oleLibrary = IntPtr.Zero;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not release one or more Windows process-loopback native libraries: {exception.Message}");
        }
    }

    /// <summary>
    /// Defines the callback signature used to report or process int information between collaborating components.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    private delegate int ActivateAudioInterfaceDelegate(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceInterfacePath,
        ref Guid interfaceId,
        IntPtr activationParameters,
        [MarshalAs(UnmanagedType.Interface)] object completionHandler,
        [MarshalAs(UnmanagedType.Interface)] out object activationOperation);

    /// <summary>
    /// Defines the callback signature used to report or process int information between collaborating components.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int CoInitializeExDelegate(IntPtr reserved, uint concurrencyModel);

    /// <summary>
    /// Defines the callback signature used to report or process void information between collaborating components.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void CoUninitializeDelegate();
}

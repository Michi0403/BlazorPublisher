using System.Runtime.InteropServices;

namespace PublisherStudio.Services.Streaming.Capture;

public interface IWindowsProcessLoopbackNativeService
{
    bool IsAvailable { get; }
    int InitializeMultithreadedApartment();
    void UninitializeApartment();
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
public sealed class WindowsProcessLoopbackNativeService : IWindowsProcessLoopbackNativeService, IDisposable
{
    private readonly ILogger<WindowsProcessLoopbackNativeService> logger;
    private IntPtr multimediaDeviceLibrary;
    private IntPtr oleLibrary;
    private ActivateAudioInterfaceDelegate? activateAudioInterface;
    private CoInitializeExDelegate? initializeApartment;
    private CoUninitializeDelegate? uninitializeApartment;
    private bool disposed;

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

    public bool IsAvailable { get; private set; }

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

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    private delegate int ActivateAudioInterfaceDelegate(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceInterfacePath,
        ref Guid interfaceId,
        IntPtr activationParameters,
        [MarshalAs(UnmanagedType.Interface)] object completionHandler,
        [MarshalAs(UnmanagedType.Interface)] out object activationOperation);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int CoInitializeExDelegate(IntPtr reserved, uint concurrencyModel);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void CoUninitializeDelegate();
}

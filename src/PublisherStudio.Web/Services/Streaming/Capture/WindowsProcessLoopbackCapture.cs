using PublisherStudio.Services.Configuration;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PublisherStudio.Services.Streaming.Capture;

/// <summary>
/// Defines the contract for windows process loopback capture behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IProcessLoopbackCapture : IDisposable
{
    /// <summary>
    /// Performs start for <see cref="IProcessLoopbackCapture"/>, keeping the operation consistent with the state and invariants of the surrounding windows process loopback capture workflow.
    /// </summary>
    void Start();
}

/// <summary>
/// Defines the contract for windows process loopback capture behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IProcessLoopbackCaptureFactory
{
    /// <summary>
    /// Performs create using the configuration and dependencies owned by <see cref="IProcessLoopbackCaptureFactory"/>.
    /// </summary>
    /// <param name="processId">Identifier of the process to use for this operation.</param>
    /// <param name="destination">Destination value supplied to the windows process loopback capture operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The i windows process loopback capture produced by the operation.</returns>
    IProcessLoopbackCapture Create(uint processId, Stream destination, CancellationToken cancellationToken);
}

/// <summary>
/// Creates configured windows process loopback capture instances from the application's current dependencies and runtime settings.
/// </summary>
/// <param name="nativeService">Windows process loopback native service dependency used by the windows process loopback capture workflow to provide the corresponding application capability.</param>
/// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the windows process loopback capture workflow to provide the corresponding application capability.</param>
/// <param name="loggerFactory">Logger factory dependency used by the windows process loopback capture workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class WindowsProcessLoopbackCaptureFactory(
    IProcessLoopbackNativeService nativeService,
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILoggerFactory loggerFactory,
    ILogger<WindowsProcessLoopbackCaptureFactory> logger) : IProcessLoopbackCaptureFactory
{
    /// <summary>
    /// Performs create using the configuration and dependencies owned by <see cref="WindowsProcessLoopbackCaptureFactory"/>.
    /// </summary>
    /// <param name="processId">Identifier of the process to use for this operation.</param>
    /// <param name="destination">Destination value supplied to the windows process loopback capture operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The i windows process loopback capture produced by the operation.</returns>
    public IProcessLoopbackCapture Create(uint processId, Stream destination, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Process audio loopback is available only on Windows.");

        try
        {
            logger.LogTrace("Creating Windows process loopback capture for process {ProcessId}.", processId);
            return new WindowsProcessLoopbackCapture(
                processId,
                destination,
                cancellationToken,
                nativeService,
                runtimePolicy.AudioClientInterfaceId,
                runtimePolicy.AudioCaptureClientInterfaceId,
                loggerFactory.CreateLogger<WindowsProcessLoopbackCapture>());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create Windows process loopback capture for process {ProcessId}.", processId);
            throw;
        }
    }
}

/// <summary>
/// Windows 10 build 20348+ process-tree loopback capture. The implementation
/// follows the public ApplicationLoopback sample but writes fixed 48 kHz,
/// stereo, 16-bit PCM into the integrated streaming runtime's FFmpeg stdin instead of a WAV file.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsProcessLoopbackCapture : IProcessLoopbackCapture
{
    /// <summary>
    /// Defines the virtual audio device process loopback constant used by <see cref="WindowsProcessLoopbackCapture"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const string VirtualAudioDeviceProcessLoopback = "VAD\\Process_Loopback";
    /// <summary>
    /// Defines the audclnt streamflags loopback constant used by <see cref="WindowsProcessLoopbackCapture"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint AudclntStreamflagsLoopback = 0x00020000;
    /// <summary>
    /// Defines the audclnt streamflags event callback constant used by <see cref="WindowsProcessLoopbackCapture"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint AudclntStreamflagsEventCallback = 0x00040000;
    /// <summary>
    /// Defines the audclnt streamflags src default quality constant used by <see cref="WindowsProcessLoopbackCapture"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint AudclntStreamflagsSrcDefaultQuality = 0x08000000;
    /// <summary>
    /// Defines the audclnt streamflags auto convert pcm constant used by <see cref="WindowsProcessLoopbackCapture"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint AudclntStreamflagsAutoConvertPcm = 0x80000000;
    /// <summary>
    /// Defines the audclnt bufferflags silent constant used by <see cref="WindowsProcessLoopbackCapture"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const uint AudclntBufferflagsSilent = 0x00000002;
    /// <summary>
    /// Defines the vt blob constant used by <see cref="WindowsProcessLoopbackCapture"/> so callers and internal logic share the same stable value.
    /// </summary>
    private const ushort VtBlob = 65;

    /// <summary>
    /// Stores the internal audio client interface identifier state used by <see cref="WindowsProcessLoopbackCapture"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Guid _audioClientInterfaceId;
    /// <summary>
    /// Stores the logger used by <see cref="WindowsProcessLoopbackCapture"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<WindowsProcessLoopbackCapture> _logger;
    /// <summary>
    /// Stores the internal audio capture client interface identifier state used by <see cref="WindowsProcessLoopbackCapture"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Guid _audioCaptureClientInterfaceId;
    /// <summary>
    /// Stores the windows process loopback native service dependency used by <see cref="WindowsProcessLoopbackCapture"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IProcessLoopbackNativeService _nativeService;
    /// <summary>
    /// Stores the internal process identifier state used by <see cref="WindowsProcessLoopbackCapture"/> while executing its surrounding workflow.
    /// </summary>
    private readonly uint _processId;
    /// <summary>
    /// Stores the internal destination state used by <see cref="WindowsProcessLoopbackCapture"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Stream _destination;
    /// <summary>
    /// Stores the cancellation source used by <see cref="WindowsProcessLoopbackCapture"/> to stop its current background or asynchronous operation.
    /// </summary>
    private readonly CancellationTokenSource _cancellation;
    /// <summary>
    /// Stores the internal started state used by <see cref="WindowsProcessLoopbackCapture"/> while executing its surrounding workflow.
    /// </summary>
    private readonly ManualResetEventSlim _started = new(false);
    /// <summary>
    /// Stores the internal thread state used by <see cref="WindowsProcessLoopbackCapture"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Thread _thread;
    /// <summary>
    /// Stores the internal startup error state used by <see cref="WindowsProcessLoopbackCapture"/> while executing its surrounding workflow.
    /// </summary>
    private Exception? _startupError;
    /// <summary>
    /// Stores the internal disposed state used by <see cref="WindowsProcessLoopbackCapture"/> while executing its surrounding workflow.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="WindowsProcessLoopbackCapture"/> instance and captures the dependencies or initial state required by its windows process loopback capture workflow.
    /// </summary>
    /// <param name="processId">Identifier of the process to use for this operation.</param>
    /// <param name="destination">Destination value supplied to the windows process loopback capture operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="nativeService">Windows process loopback native service dependency used by the windows process loopback capture workflow to provide the corresponding application capability.</param>
    /// <param name="audioClientInterfaceId">Identifier of the audio client interface to use for this operation.</param>
    /// <param name="audioCaptureClientInterfaceId">Identifier of the audio capture client interface to use for this operation.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public WindowsProcessLoopbackCapture(
        uint processId,
        Stream destination,
        CancellationToken cancellationToken,
        IProcessLoopbackNativeService nativeService,
        Guid audioClientInterfaceId,
        Guid audioCaptureClientInterfaceId,
        ILogger<WindowsProcessLoopbackCapture> logger)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Process audio loopback is available only on Windows.");
        _processId = processId;
        _nativeService = nativeService;
        _audioClientInterfaceId = audioClientInterfaceId;
        _audioCaptureClientInterfaceId = audioCaptureClientInterfaceId;
        _logger = logger;
        _destination = destination;
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _thread = new Thread(CaptureThread)
        {
            IsBackground = true,
            Name = $"PublisherStudio process audio {_processId}"
        };
    }

    /// <summary>
    /// Performs start for <see cref="WindowsProcessLoopbackCapture"/>, keeping the operation consistent with the state and invariants of the surrounding windows process loopback capture workflow.
    /// </summary>
    public void Start()
    {
    try
    {
            _thread.Start();
            if (!_started.Wait(TimeSpan.FromSeconds(15)))
                throw new TimeoutException("Windows did not initialize process audio loopback in time.");
            if (_startupError is not null)
                throw new InvalidOperationException("Windows process audio loopback could not start.", _startupError);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            _logger.LogDebug(__serviceMethodException, $"Service method {nameof(WindowsProcessLoopbackCapture)}.{nameof(Start)} was canceled.");
        else
            _logger.LogError(__serviceMethodException, $"Service method {nameof(WindowsProcessLoopbackCapture)}.{nameof(Start)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs capture thread for <see cref="WindowsProcessLoopbackCapture"/>, keeping the operation consistent with the state and invariants of the surrounding windows process loopback capture workflow.
    /// </summary>
    private void CaptureThread()
    {
        IAudioClient? audioClient = null;
        IAudioCaptureClient? captureClient = null;
        EventWaitHandle? sampleReady = null;
        var coInitialized = false;
        try
        {
            var initializationResult = _nativeService.InitializeMultithreadedApartment();
            ThrowIfFailed(initializationResult);
            coInitialized = true;
            audioClient = ActivateAudioClient(_processId);
            var format = new WaveFormatEx
            {
                FormatTag = 1,
                Channels = 2,
                SamplesPerSec = 48000,
                BitsPerSample = 16,
                BlockAlign = 4,
                AvgBytesPerSec = 48000 * 4,
                Size = 0
            };
            var formatPointer = Marshal.AllocCoTaskMem(Marshal.SizeOf<WaveFormatEx>());
            try
            {
                Marshal.StructureToPtr(format, formatPointer, false);
                ThrowIfFailed(audioClient.Initialize(
                    0,
                    AudclntStreamflagsLoopback | AudclntStreamflagsEventCallback | AudclntStreamflagsAutoConvertPcm | AudclntStreamflagsSrcDefaultQuality,
                    0,
                    0,
                    formatPointer,
                    IntPtr.Zero));
            }
            finally { Marshal.FreeCoTaskMem(formatPointer); }

            sampleReady = new EventWaitHandle(false, EventResetMode.AutoReset);
            ThrowIfFailed(audioClient.SetEventHandle(sampleReady.SafeWaitHandle.DangerousGetHandle()));
            var captureClientId = _audioCaptureClientInterfaceId;
            ThrowIfFailed(audioClient.GetService(ref captureClientId, out var service));
            captureClient = (IAudioCaptureClient)service;
            ThrowIfFailed(audioClient.Start());
            _started.Set();

            var waits = new WaitHandle[] { sampleReady, _cancellation.Token.WaitHandle };
            var silence = Array.Empty<byte>();
            while (!_cancellation.IsCancellationRequested)
            {
                if (WaitHandle.WaitAny(waits, 1000) == 1) break;
                ThrowIfFailed(captureClient.GetNextPacketSize(out var packetFrames));
                while (packetFrames > 0 && !_cancellation.IsCancellationRequested)
                {
                    ThrowIfFailed(captureClient.GetBuffer(out var data, out var frames, out var flags, out _, out _));
                    try
                    {
                        var byteCount = checked((int)frames * format.BlockAlign);
                        if ((flags & AudclntBufferflagsSilent) != 0 || data == IntPtr.Zero)
                        {
                            if (silence.Length < byteCount) silence = new byte[byteCount];
                            _destination.Write(silence, 0, byteCount);
                        }
                        else
                        {
                            var buffer = new byte[byteCount];
                            Marshal.Copy(data, buffer, 0, byteCount);
                            _destination.Write(buffer, 0, buffer.Length);
                        }
                    }
                    finally { captureClient.ReleaseBuffer(frames); }
                    ThrowIfFailed(captureClient.GetNextPacketSize(out packetFrames));
                }
                _destination.Flush();
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Windows process loopback capture failed for process {ProcessId}.", _processId);
            if (!_started.IsSet) _startupError = exception;
        }
        finally
        {
            if (!_started.IsSet) _started.Set();
            try { audioClient?.Stop(); } catch { }
            sampleReady?.Dispose();
            ReleaseComObject(captureClient);
            ReleaseComObject(audioClient);
            if (coInitialized) _nativeService.UninitializeApartment();
            try { _destination.Flush(); } catch { }
        }
    }

    /// <summary>
    /// Performs activate audio client for <see cref="WindowsProcessLoopbackCapture"/>, keeping the operation consistent with the state and invariants of the surrounding windows process loopback capture workflow.
    /// </summary>
    /// <param name="processId">Identifier of the process to use for this operation.</param>
    /// <returns>The i audio client produced by the operation.</returns>
    private IAudioClient ActivateAudioClient(uint processId)
    {
    try
    {
            var parameters = new AudioClientActivationParams
            {
                ActivationType = 1,
                ProcessLoopbackParams = new AudioClientProcessLoopbackParams
                {
                    TargetProcessId = processId,
                    ProcessLoopbackMode = 0
                }
            };
            var parametersPointer = Marshal.AllocCoTaskMem(Marshal.SizeOf<AudioClientActivationParams>());
            var propertyPointer = Marshal.AllocCoTaskMem(Marshal.SizeOf<PropVariant>());
            try
            {
                Marshal.StructureToPtr(parameters, parametersPointer, false);
                var property = new PropVariant
                {
                    VariantType = VtBlob,
                    Blob = new Blob
                    {
                        Size = (uint)Marshal.SizeOf<AudioClientActivationParams>(),
                        Data = parametersPointer
                    }
                };
                Marshal.StructureToPtr(property, propertyPointer, false);
                var completion = new AudioActivationCompletionHandler(this);
                var audioClientId = _audioClientInterfaceId;
                ThrowIfFailed(_nativeService.ActivateAudioInterface(
                    VirtualAudioDeviceProcessLoopback,
                    ref audioClientId,
                    propertyPointer,
                    completion,
                    out var activationOperation));
                try { return completion.Wait(TimeSpan.FromSeconds(12)); }
                finally { ReleaseComObject(activationOperation); }
            }
            finally
            {
                Marshal.FreeCoTaskMem(propertyPointer);
                Marshal.FreeCoTaskMem(parametersPointer);
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            _logger.LogDebug(__serviceMethodException, $"Service method {nameof(WindowsProcessLoopbackCapture)}.{nameof(ActivateAudioClient)} was canceled.");
        else
            _logger.LogError(__serviceMethodException, $"Service method {nameof(WindowsProcessLoopbackCapture)}.{nameof(ActivateAudioClient)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs throw if failed for <see cref="WindowsProcessLoopbackCapture"/>, keeping the operation consistent with the state and invariants of the surrounding windows process loopback capture workflow.
    /// </summary>
    /// <param name="hresult">Hresult value supplied to the windows process loopback capture operation and used when producing its result.</param>
    private void ThrowIfFailed(int hresult)
    {
    try
    {
            if (hresult < 0) Marshal.ThrowExceptionForHR(hresult);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            _logger.LogDebug(__serviceMethodException, $"Service method {nameof(WindowsProcessLoopbackCapture)}.{nameof(ThrowIfFailed)} was canceled.");
        else
            _logger.LogError(__serviceMethodException, $"Service method {nameof(WindowsProcessLoopbackCapture)}.{nameof(ThrowIfFailed)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs release com object for <see cref="WindowsProcessLoopbackCapture"/>, keeping the operation consistent with the state and invariants of the surrounding windows process loopback capture workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the windows process loopback capture operation and used when producing its result.</param>
    private void ReleaseComObject(object? value)
    {
    try
    {
            if (value is null || !Marshal.IsComObject(value)) return;
            try { Marshal.FinalReleaseComObject(value); } catch { }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            _logger.LogDebug(__serviceMethodException, $"Service method {nameof(WindowsProcessLoopbackCapture)}.{nameof(ReleaseComObject)} was canceled.");
        else
            _logger.LogError(__serviceMethodException, $"Service method {nameof(WindowsProcessLoopbackCapture)}.{nameof(ReleaseComObject)} failed.");
        throw;
    }
}

    /// <summary>
    /// Releases resources owned by <see cref="WindowsProcessLoopbackCapture"/> and leaves the windows process loopback capture workflow in a safely disposed state.
    /// </summary>
    public void Dispose()
    {
    try
    {
            if (_disposed) return;
            _disposed = true;
            _cancellation.Cancel();
            if (_thread.IsAlive) _thread.Join(TimeSpan.FromSeconds(3));
            _started.Dispose();
            _cancellation.Dispose();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            _logger.LogDebug(__serviceMethodException, $"Service method {nameof(WindowsProcessLoopbackCapture)}.{nameof(Dispose)} was canceled.");
        else
            _logger.LogError(__serviceMethodException, $"Service method {nameof(WindowsProcessLoopbackCapture)}.{nameof(Dispose)} failed.");
        throw;
    }
}

    /// <summary>
    /// Represents an audio activation completion helper type nested within <see cref="WindowsProcessLoopbackCapture"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="owner">Owner value supplied to the windows process loopback capture operation and used when producing its result.</param>
    private sealed class AudioActivationCompletionHandler(WindowsProcessLoopbackCapture owner)
        : IActivateAudioInterfaceCompletionHandler
    {
        /// <summary>
        /// Stores the internal completion state used by <see cref="AudioActivationCompletionHandler"/> while executing its surrounding workflow.
        /// </summary>
        private readonly TaskCompletionSource<IAudioClient> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Performs activate completed for <see cref="AudioActivationCompletionHandler"/>, keeping the operation consistent with the state and invariants of the surrounding audio activation completion workflow.
        /// </summary>
        /// <param name="operation">Activate audio interface async operation dependency used by the audio activation completion workflow to provide the corresponding application capability.</param>
        /// <returns>The int produced by the operation.</returns>
        public int ActivateCompleted(IActivateAudioInterfaceAsyncOperation operation)
        {
    try
    {
                try
                {
                    owner.ThrowIfFailed(operation.GetActivateResult(out var activationResult, out var activated));
                    owner.ThrowIfFailed(activationResult);
                    _completion.TrySetResult((IAudioClient)activated);
                }
                catch (Exception exception) { _completion.TrySetException(exception); }
                return 0;
        
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method AudioActivationCompletionHandler.ActivateCompleted failed: {__serviceMethodException}");
        throw;
    }
}

        /// <summary>
        /// Performs wait for <see cref="AudioActivationCompletionHandler"/>, keeping the operation consistent with the state and invariants of the surrounding audio activation completion workflow.
        /// </summary>
        /// <param name="timeout">Timeout value supplied to the audio activation completion operation and used when producing its result.</param>
        /// <returns>The i audio client produced by the operation.</returns>
        public IAudioClient Wait(TimeSpan timeout)
        {
    try
    {
                if (!_completion.Task.Wait(timeout)) throw new TimeoutException("Windows process audio activation timed out.");
                return _completion.Task.GetAwaiter().GetResult();
        
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method AudioActivationCompletionHandler.Wait failed: {__serviceMethodException}");
        throw;
    }
}
    }

    /// <summary>
    /// Represents an audio client activation params helper type nested within <see cref="WindowsProcessLoopbackCapture"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct AudioClientActivationParams
    {
        /// <summary>
        /// Stores the internal activation type state used by <see cref="AudioClientActivationParams"/> while executing its surrounding workflow.
        /// </summary>
        public int ActivationType;
        /// <summary>
        /// Stores the internal process loopback params state used by <see cref="AudioClientActivationParams"/> while executing its surrounding workflow.
        /// </summary>
        public AudioClientProcessLoopbackParams ProcessLoopbackParams;
    }

    /// <summary>
    /// Represents an audio client process loopback params helper type nested within <see cref="WindowsProcessLoopbackCapture"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct AudioClientProcessLoopbackParams
    {
        /// <summary>
        /// Stores the internal target process identifier state used by <see cref="AudioClientProcessLoopbackParams"/> while executing its surrounding workflow.
        /// </summary>
        public uint TargetProcessId;
        /// <summary>
        /// Stores the internal process loopback mode state used by <see cref="AudioClientProcessLoopbackParams"/> while executing its surrounding workflow.
        /// </summary>
        public int ProcessLoopbackMode;
    }

    /// <summary>
    /// Represents a prop variant helper type nested within <see cref="WindowsProcessLoopbackCapture"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        /// <summary>
        /// Stores the internal variant type state used by <see cref="PropVariant"/> while executing its surrounding workflow.
        /// </summary>
        [FieldOffset(0)] public ushort VariantType;
        /// <summary>
        /// Stores the internal blob state used by <see cref="PropVariant"/> while executing its surrounding workflow.
        /// </summary>
        [FieldOffset(8)] public Blob Blob;
    }

    /// <summary>
    /// Represents a blob helper type nested within <see cref="WindowsProcessLoopbackCapture"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Blob
    {
        /// <summary>
        /// Stores the internal size state used by <see cref="Blob"/> while executing its surrounding workflow.
        /// </summary>
        public uint Size;
        /// <summary>
        /// Stores the internal data state used by <see cref="Blob"/> while executing its surrounding workflow.
        /// </summary>
        public IntPtr Data;
    }

    /// <summary>
    /// Represents a wave format ex helper type nested within <see cref="WindowsProcessLoopbackCapture"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct WaveFormatEx
    {
        /// <summary>
        /// Stores the internal format tag state used by <see cref="WaveFormatEx"/> while executing its surrounding workflow.
        /// </summary>
        public ushort FormatTag;
        /// <summary>
        /// Stores the internal channels state used by <see cref="WaveFormatEx"/> while executing its surrounding workflow.
        /// </summary>
        public ushort Channels;
        /// <summary>
        /// Stores the internal samples per sec state used by <see cref="WaveFormatEx"/> while executing its surrounding workflow.
        /// </summary>
        public uint SamplesPerSec;
        /// <summary>
        /// Stores the internal avg bytes per sec state used by <see cref="WaveFormatEx"/> while executing its surrounding workflow.
        /// </summary>
        public uint AvgBytesPerSec;
        /// <summary>
        /// Stores the synchronization primitive that protects concurrent access to block align state owned by <see cref="WaveFormatEx"/>.
        /// </summary>
        public ushort BlockAlign;
        /// <summary>
        /// Stores the internal bits per sample state used by <see cref="WaveFormatEx"/> while executing its surrounding workflow.
        /// </summary>
        public ushort BitsPerSample;
        /// <summary>
        /// Stores the internal size state used by <see cref="WaveFormatEx"/> while executing its surrounding workflow.
        /// </summary>
        public ushort Size;
    }

    /// <summary>
    /// Defines the contract for activate audio interface completion behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    [ComImport, Guid("41D949AB-9862-444A-80F6-C261334DA5EB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivateAudioInterfaceCompletionHandler
    {
        /// <summary>
        /// Performs activate completed for <see cref="IActivateAudioInterfaceCompletionHandler"/>, keeping the operation consistent with the state and invariants of the surrounding activate audio interface completion workflow.
        /// </summary>
        /// <param name="operation">Activate audio interface async operation dependency used by the activate audio interface completion workflow to provide the corresponding application capability.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig]
        int ActivateCompleted([MarshalAs(UnmanagedType.Interface)] IActivateAudioInterfaceAsyncOperation operation);
    }

    /// <summary>
    /// Defines the contract for activate audio interface async operation behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    [ComImport, Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivateAudioInterfaceAsyncOperation
    {
        /// <summary>
        /// Retrieves activate result for <see cref="IActivateAudioInterfaceAsyncOperation"/>, keeping the operation consistent with the state and invariants of the surrounding activate audio interface async operation workflow.
        /// </summary>
        /// <param name="activateResult">Activate result value supplied to the activate audio interface async operation operation and used when producing its result.</param>
        /// <param name="activatedInterface">Activated interface value supplied to the activate audio interface async operation operation and used when producing its result.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig]
        int GetActivateResult(out int activateResult, [MarshalAs(UnmanagedType.IUnknown)] out object activatedInterface);
    }

    /// <summary>
    /// Defines the contract for audio behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    [ComImport, Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioClient
    {
        /// <summary>
        /// Performs initialize for <see cref="IAudioClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio workflow.
        /// </summary>
        /// <param name="shareMode">Share mode value supplied to the audio operation and used when producing its result.</param>
        /// <param name="streamFlags">Stream flags value supplied to the audio operation and used when producing its result.</param>
        /// <param name="bufferDuration">Buffer duration value supplied to the audio operation and used when producing its result.</param>
        /// <param name="periodicity">Periodicity value supplied to the audio operation and used when producing its result.</param>
        /// <param name="format">Int ptr dependency used by the audio workflow to provide the corresponding application capability.</param>
        /// <param name="audioSessionGuid">Identifier of the audio session gu to use for this operation.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int Initialize(int shareMode, uint streamFlags, long bufferDuration, long periodicity, IntPtr format, IntPtr audioSessionGuid);
        /// <summary>
        /// Retrieves buffer size for <see cref="IAudioClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio workflow.
        /// </summary>
        /// <param name="bufferFrames">Buffer frames value supplied to the audio operation and used when producing its result.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int GetBufferSize(out uint bufferFrames);
        /// <summary>
        /// Retrieves stream latency for <see cref="IAudioClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio workflow.
        /// </summary>
        /// <param name="latency">Latency value supplied to the audio operation and used when producing its result.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int GetStreamLatency(out long latency);
        /// <summary>
        /// Retrieves current padding for <see cref="IAudioClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio workflow.
        /// </summary>
        /// <param name="currentPadding">Current padding value supplied to the audio operation and used when producing its result.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int GetCurrentPadding(out uint currentPadding);
        /// <summary>
        /// Determines whether format supported for <see cref="IAudioClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio workflow.
        /// </summary>
        /// <param name="shareMode">Share mode value supplied to the audio operation and used when producing its result.</param>
        /// <param name="format">Int ptr dependency used by the audio workflow to provide the corresponding application capability.</param>
        /// <param name="closestMatch">Int ptr dependency used by the audio workflow to provide the corresponding application capability.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int IsFormatSupported(int shareMode, IntPtr format, out IntPtr closestMatch);
        /// <summary>
        /// Retrieves mix format for <see cref="IAudioClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio workflow.
        /// </summary>
        /// <param name="deviceFormat">Int ptr dependency used by the audio workflow to provide the corresponding application capability.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int GetMixFormat(out IntPtr deviceFormat);
        /// <summary>
        /// Retrieves device period for <see cref="IAudioClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio workflow.
        /// </summary>
        /// <param name="defaultDevicePeriod">Default device period value supplied to the audio operation and used when producing its result.</param>
        /// <param name="minimumDevicePeriod">Minimum device period value supplied to the audio operation and used when producing its result.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);
        /// <summary>
        /// Performs start for <see cref="IAudioClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio workflow.
        /// </summary>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int Start();
        /// <summary>
        /// Performs stop for <see cref="IAudioClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio workflow.
        /// </summary>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int Stop();
        /// <summary>
        /// Performs reset for <see cref="IAudioClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio workflow.
        /// </summary>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int Reset();
        /// <summary>
        /// Sets event handle for <see cref="IAudioClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio workflow.
        /// </summary>
        /// <param name="eventHandle">Int ptr dependency used by the audio workflow to provide the corresponding application capability.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int SetEventHandle(IntPtr eventHandle);
        /// <summary>
        /// Retrieves service for <see cref="IAudioClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio workflow.
        /// </summary>
        /// <param name="riid">Identifier of the ri to use for this operation.</param>
        /// <param name="service">Service value supplied to the audio operation and used when producing its result.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int GetService(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
    }

    /// <summary>
    /// Defines the contract for audio capture behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
    /// </summary>
    [ComImport, Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioCaptureClient
    {
        /// <summary>
        /// Retrieves buffer for <see cref="IAudioCaptureClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio capture workflow.
        /// </summary>
        /// <param name="data">Int ptr dependency used by the audio capture workflow to provide the corresponding application capability.</param>
        /// <param name="frames">Frames value supplied to the audio capture operation and used when producing its result.</param>
        /// <param name="flags">Flags value supplied to the audio capture operation and used when producing its result.</param>
        /// <param name="devicePosition">Device position value supplied to the audio capture operation and used when producing its result.</param>
        /// <param name="qpcPosition">Qpc position value supplied to the audio capture operation and used when producing its result.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int GetBuffer(out IntPtr data, out uint frames, out uint flags, out ulong devicePosition, out ulong qpcPosition);
        /// <summary>
        /// Performs release buffer for <see cref="IAudioCaptureClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio capture workflow.
        /// </summary>
        /// <param name="frames">Frames value supplied to the audio capture operation and used when producing its result.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int ReleaseBuffer(uint frames);
        /// <summary>
        /// Retrieves next packet size for <see cref="IAudioCaptureClient"/>, keeping the operation consistent with the state and invariants of the surrounding audio capture workflow.
        /// </summary>
        /// <param name="packetFrames">Packet frames value supplied to the audio capture operation and used when producing its result.</param>
        /// <returns>The int produced by the operation.</returns>
        [PreserveSig] int GetNextPacketSize(out uint packetFrames);
    }
}

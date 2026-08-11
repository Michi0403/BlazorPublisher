using PublisherStudio.Services.Configuration;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PublisherStudio.Services.Streaming.Capture;

/// <summary>
/// Defines the windows process loopback capture contract.
/// </summary>
public interface IWindowsProcessLoopbackCapture : IDisposable
{
    /// <summary>
    /// Runs the start operation.
    /// </summary>
    void Start();
}

/// <summary>
/// Defines the windows process loopback capture factory contract.
/// </summary>
public interface IWindowsProcessLoopbackCaptureFactory
{
    /// <summary>
    /// Runs the create operation.
    /// </summary>
    IWindowsProcessLoopbackCapture Create(uint processId, Stream destination, CancellationToken cancellationToken);
}

/// <summary>
/// Provides windows process loopback capture factory operations.
/// </summary>
public sealed class WindowsProcessLoopbackCaptureFactory(
    IWindowsProcessLoopbackNativeService nativeService,
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILoggerFactory loggerFactory,
    ILogger<WindowsProcessLoopbackCaptureFactory> logger) : IWindowsProcessLoopbackCaptureFactory
{
    /// <summary>
    /// Runs the create operation.
    /// </summary>
    public IWindowsProcessLoopbackCapture Create(uint processId, Stream destination, CancellationToken cancellationToken)
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
internal sealed class WindowsProcessLoopbackCapture : IWindowsProcessLoopbackCapture
{
    private const string VirtualAudioDeviceProcessLoopback = "VAD\\Process_Loopback";
    private const uint AudclntStreamflagsLoopback = 0x00020000;
    private const uint AudclntStreamflagsEventCallback = 0x00040000;
    private const uint AudclntStreamflagsSrcDefaultQuality = 0x08000000;
    private const uint AudclntStreamflagsAutoConvertPcm = 0x80000000;
    private const uint AudclntBufferflagsSilent = 0x00000002;
    private const ushort VtBlob = 65;

    private readonly Guid _audioClientInterfaceId;
    private readonly ILogger<WindowsProcessLoopbackCapture> _logger;
    private readonly Guid _audioCaptureClientInterfaceId;
    private readonly IWindowsProcessLoopbackNativeService _nativeService;
    private readonly uint _processId;
    private readonly Stream _destination;
    private readonly CancellationTokenSource _cancellation;
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly ManualResetEventSlim _started = new(false);
    private readonly Thread _thread;
    private Exception? _startupError;
    private bool _disposed;

    /// <summary>
    /// Runs the windows process loopback capture operation.
    /// </summary>
    public WindowsProcessLoopbackCapture(
        uint processId,
        Stream destination,
        CancellationToken cancellationToken,
        IWindowsProcessLoopbackNativeService nativeService,
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
    /// Runs the start operation.
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
    /// Runs the capture thread operation.
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
    /// Runs the activate audio client operation.
    /// </summary>
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
    /// Runs the throw if failed operation.
    /// </summary>
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
    /// Runs the release com object operation.
    /// </summary>
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
    /// Runs the dispose operation.
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
    /// Represents an audio activation completion handler.
    /// </summary>
    private sealed class AudioActivationCompletionHandler(WindowsProcessLoopbackCapture owner)
        : IActivateAudioInterfaceCompletionHandler
    {
        /// <summary>
        /// Runs the new operation.
        /// </summary>
        private readonly TaskCompletionSource<IAudioClient> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Runs the activate completed operation.
        /// </summary>
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
        /// Runs the wait operation.
        /// </summary>
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
    /// Represents an audio client activation params.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct AudioClientActivationParams
    {
        /// <summary>
        /// Stores activation type.
        /// </summary>
        public int ActivationType;
        /// <summary>
        /// Stores process loopback params.
        /// </summary>
        public AudioClientProcessLoopbackParams ProcessLoopbackParams;
    }

    /// <summary>
    /// Represents an audio client process loopback params.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct AudioClientProcessLoopbackParams
    {
        /// <summary>
        /// Stores target process identifier.
        /// </summary>
        public uint TargetProcessId;
        /// <summary>
        /// Stores process loopback mode.
        /// </summary>
        public int ProcessLoopbackMode;
    }

    /// <summary>
    /// Represents a prop variant.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        /// <summary>
        /// Stores variant type.
        /// </summary>
        [FieldOffset(0)] public ushort VariantType;
        /// <summary>
        /// Stores blob.
        /// </summary>
        [FieldOffset(8)] public Blob Blob;
    }

    /// <summary>
    /// Represents a blob.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Blob
    {
        /// <summary>
        /// Stores size.
        /// </summary>
        public uint Size;
        /// <summary>
        /// Stores data.
        /// </summary>
        public IntPtr Data;
    }

    /// <summary>
    /// Represents a wave format ex.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct WaveFormatEx
    {
        /// <summary>
        /// Stores format tag.
        /// </summary>
        public ushort FormatTag;
        /// <summary>
        /// Stores channels.
        /// </summary>
        public ushort Channels;
        /// <summary>
        /// Stores samples per sec.
        /// </summary>
        public uint SamplesPerSec;
        /// <summary>
        /// Stores avg bytes per sec.
        /// </summary>
        public uint AvgBytesPerSec;
        /// <summary>
        /// Stores block align.
        /// </summary>
        public ushort BlockAlign;
        /// <summary>
        /// Stores bits per sample.
        /// </summary>
        public ushort BitsPerSample;
        /// <summary>
        /// Stores size.
        /// </summary>
        public ushort Size;
    }

    /// <summary>
    /// Defines the activate audio interface completion handler contract.
    /// </summary>
    [ComImport, Guid("41D949AB-9862-444A-80F6-C261334DA5EB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivateAudioInterfaceCompletionHandler
    {
        /// <summary>
        /// Runs the activate completed operation.
        /// </summary>
        [PreserveSig]
        int ActivateCompleted([MarshalAs(UnmanagedType.Interface)] IActivateAudioInterfaceAsyncOperation operation);
    }

    /// <summary>
    /// Defines the activate audio interface async operation contract.
    /// </summary>
    [ComImport, Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivateAudioInterfaceAsyncOperation
    {
        /// <summary>
        /// Gets activate result.
        /// </summary>
        [PreserveSig]
        int GetActivateResult(out int activateResult, [MarshalAs(UnmanagedType.IUnknown)] out object activatedInterface);
    }

    /// <summary>
    /// Defines the audio client contract.
    /// </summary>
    [ComImport, Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioClient
    {
       /// <summary>
       /// Runs the initialize operation.
       /// </summary>
        [PreserveSig] int Initialize(int shareMode, uint streamFlags, long bufferDuration, long periodicity, IntPtr format, IntPtr audioSessionGuid);
       /// <summary>
       /// Gets buffer size.
       /// </summary>
        [PreserveSig] int GetBufferSize(out uint bufferFrames);
       /// <summary>
       /// Gets stream latency.
       /// </summary>
        [PreserveSig] int GetStreamLatency(out long latency);
       /// <summary>
       /// Gets current padding.
       /// </summary>
        [PreserveSig] int GetCurrentPadding(out uint currentPadding);
       /// <summary>
       /// Determines whether format supported.
       /// </summary>
        [PreserveSig] int IsFormatSupported(int shareMode, IntPtr format, out IntPtr closestMatch);
       /// <summary>
       /// Gets mix format.
       /// </summary>
        [PreserveSig] int GetMixFormat(out IntPtr deviceFormat);
       /// <summary>
       /// Gets device period.
       /// </summary>
        [PreserveSig] int GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);
       /// <summary>
       /// Runs the start operation.
       /// </summary>
        [PreserveSig] int Start();
       /// <summary>
       /// Runs the stop operation.
       /// </summary>
        [PreserveSig] int Stop();
       /// <summary>
       /// Runs the reset operation.
       /// </summary>
        [PreserveSig] int Reset();
       /// <summary>
       /// Sets event handle.
       /// </summary>
        [PreserveSig] int SetEventHandle(IntPtr eventHandle);
       /// <summary>
       /// Gets service.
       /// </summary>
        [PreserveSig] int GetService(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
    }

    /// <summary>
    /// Defines the audio capture client contract.
    /// </summary>
    [ComImport, Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioCaptureClient
    {
       /// <summary>
       /// Gets buffer.
       /// </summary>
        [PreserveSig] int GetBuffer(out IntPtr data, out uint frames, out uint flags, out ulong devicePosition, out ulong qpcPosition);
       /// <summary>
       /// Runs the release buffer operation.
       /// </summary>
        [PreserveSig] int ReleaseBuffer(uint frames);
       /// <summary>
       /// Gets next packet size.
       /// </summary>
        [PreserveSig] int GetNextPacketSize(out uint packetFrames);
    }
}

using System.Runtime.InteropServices;

/// <summary>
/// Windows 10 build 20348+ process-tree loopback capture. The implementation
/// follows the public ApplicationLoopback sample but writes fixed 48 kHz,
/// stereo, 16-bit PCM into the Media Host's FFmpeg stdin instead of a WAV file.
/// </summary>
internal sealed class WindowsProcessLoopbackCapture : IDisposable
{
    private const string VirtualAudioDeviceProcessLoopback = "VAD\\Process_Loopback";
    private const uint AudclntStreamflagsLoopback = 0x00020000;
    private const uint AudclntStreamflagsEventCallback = 0x00040000;
    private const uint AudclntStreamflagsSrcDefaultQuality = 0x08000000;
    private const uint AudclntStreamflagsAutoConvertPcm = 0x80000000;
    private const uint AudclntBufferflagsSilent = 0x00000002;
    private const ushort VtBlob = 65;
    private const uint CoinitMultithreaded = 0x0;
    private static readonly Guid IidAudioClient = new("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
    private static readonly Guid IidAudioCaptureClient = new("C8ADBD64-E71E-48A0-A4DE-185C395CD317");

    private readonly uint _processId;
    private readonly Stream _destination;
    private readonly CancellationTokenSource _cancellation;
    private readonly ManualResetEventSlim _started = new(false);
    private readonly Thread _thread;
    private Exception? _startupError;
    private bool _disposed;

    public WindowsProcessLoopbackCapture(uint processId, Stream destination, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Process audio loopback is available only on Windows.");
        _processId = processId;
        _destination = destination;
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _thread = new Thread(CaptureThread)
        {
            IsBackground = true,
            Name = $"PublisherStudio process audio {_processId}"
        };
    }

    public void Start()
    {
        _thread.Start();
        if (!_started.Wait(TimeSpan.FromSeconds(15)))
            throw new TimeoutException("Windows did not initialize process audio loopback in time.");
        if (_startupError is not null)
            throw new InvalidOperationException("Windows process audio loopback could not start.", _startupError);
    }

    private void CaptureThread()
    {
        IAudioClient? audioClient = null;
        IAudioCaptureClient? captureClient = null;
        EventWaitHandle? sampleReady = null;
        var coInitialized = CoInitializeEx(IntPtr.Zero, CoinitMultithreaded) >= 0;
        try
        {
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
            var captureClientId = IidAudioCaptureClient;
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
            if (!_started.IsSet) _startupError = exception;
        }
        finally
        {
            if (!_started.IsSet) _started.Set();
            try { audioClient?.Stop(); } catch { }
            sampleReady?.Dispose();
            ReleaseComObject(captureClient);
            ReleaseComObject(audioClient);
            if (coInitialized) CoUninitialize();
            try { _destination.Flush(); } catch { }
        }
    }

    private static IAudioClient ActivateAudioClient(uint processId)
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
            var completion = new AudioActivationCompletionHandler();
            var audioClientId = IidAudioClient;
            ThrowIfFailed(ActivateAudioInterfaceAsync(
                VirtualAudioDeviceProcessLoopback,
                ref audioClientId,
                propertyPointer,
                completion,
                out var operation));
            try { return completion.Wait(TimeSpan.FromSeconds(12)); }
            finally { ReleaseComObject(operation); }
        }
        finally
        {
            Marshal.FreeCoTaskMem(propertyPointer);
            Marshal.FreeCoTaskMem(parametersPointer);
        }
    }

    private static void ThrowIfFailed(int hresult)
    {
        if (hresult < 0) Marshal.ThrowExceptionForHR(hresult);
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is null || !Marshal.IsComObject(value)) return;
        try { Marshal.FinalReleaseComObject(value); } catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cancellation.Cancel();
        if (_thread.IsAlive) _thread.Join(TimeSpan.FromSeconds(3));
        _started.Dispose();
        _cancellation.Dispose();
    }

    private sealed class AudioActivationCompletionHandler : IActivateAudioInterfaceCompletionHandler
    {
        private readonly TaskCompletionSource<IAudioClient> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ActivateCompleted(IActivateAudioInterfaceAsyncOperation operation)
        {
            try
            {
                ThrowIfFailed(operation.GetActivateResult(out var activationResult, out var activated));
                ThrowIfFailed(activationResult);
                _completion.TrySetResult((IAudioClient)activated);
            }
            catch (Exception exception) { _completion.TrySetException(exception); }
            return 0;
        }

        public IAudioClient Wait(TimeSpan timeout)
        {
            if (!_completion.Task.Wait(timeout)) throw new TimeoutException("Windows process audio activation timed out.");
            return _completion.Task.GetAwaiter().GetResult();
        }
    }

    [DllImport("Mmdevapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int ActivateAudioInterfaceAsync(
        string deviceInterfacePath,
        ref Guid riid,
        IntPtr activationParams,
        [MarshalAs(UnmanagedType.Interface)] IActivateAudioInterfaceCompletionHandler completionHandler,
        [MarshalAs(UnmanagedType.Interface)] out IActivateAudioInterfaceAsyncOperation activationOperation);

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr reserved, uint coInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioClientActivationParams
    {
        public int ActivationType;
        public AudioClientProcessLoopbackParams ProcessLoopbackParams;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioClientProcessLoopbackParams
    {
        public uint TargetProcessId;
        public int ProcessLoopbackMode;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)] public ushort VariantType;
        [FieldOffset(8)] public Blob Blob;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Blob
    {
        public uint Size;
        public IntPtr Data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct WaveFormatEx
    {
        public ushort FormatTag;
        public ushort Channels;
        public uint SamplesPerSec;
        public uint AvgBytesPerSec;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        public ushort Size;
    }

    [ComImport, Guid("41D949AB-9862-444A-80F6-C261334DA5EB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivateAudioInterfaceCompletionHandler
    {
        [PreserveSig]
        int ActivateCompleted([MarshalAs(UnmanagedType.Interface)] IActivateAudioInterfaceAsyncOperation operation);
    }

    [ComImport, Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivateAudioInterfaceAsyncOperation
    {
        [PreserveSig]
        int GetActivateResult(out int activateResult, [MarshalAs(UnmanagedType.IUnknown)] out object activatedInterface);
    }

    [ComImport, Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioClient
    {
        [PreserveSig] int Initialize(int shareMode, uint streamFlags, long bufferDuration, long periodicity, IntPtr format, IntPtr audioSessionGuid);
        [PreserveSig] int GetBufferSize(out uint bufferFrames);
        [PreserveSig] int GetStreamLatency(out long latency);
        [PreserveSig] int GetCurrentPadding(out uint currentPadding);
        [PreserveSig] int IsFormatSupported(int shareMode, IntPtr format, out IntPtr closestMatch);
        [PreserveSig] int GetMixFormat(out IntPtr deviceFormat);
        [PreserveSig] int GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);
        [PreserveSig] int Start();
        [PreserveSig] int Stop();
        [PreserveSig] int Reset();
        [PreserveSig] int SetEventHandle(IntPtr eventHandle);
        [PreserveSig] int GetService(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
    }

    [ComImport, Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioCaptureClient
    {
        [PreserveSig] int GetBuffer(out IntPtr data, out uint frames, out uint flags, out ulong devicePosition, out ulong qpcPosition);
        [PreserveSig] int ReleaseBuffer(uint frames);
        [PreserveSig] int GetNextPacketSize(out uint packetFrames);
    }
}

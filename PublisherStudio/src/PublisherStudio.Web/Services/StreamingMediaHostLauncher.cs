using System.Diagnostics;

namespace PublisherStudio.Services;

public sealed class StreamingMediaHostLauncher(StreamingProfileStore profiles, ILogger<StreamingMediaHostLauncher> logger) : IHostedService, IDisposable
{
    private readonly StreamingProfileStore _profiles = profiles;
    private readonly ILogger<StreamingMediaHostLauncher> _logger = logger;
    private Process? _process;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = await _profiles.LoadAsync(cancellationToken);
        var executable = OperatingSystem.IsWindows() ? "PublisherStudio.MediaHost.exe" : "PublisherStudio.MediaHost";
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "MediaHost", executable),
            Path.Combine(AppContext.BaseDirectory, executable)
        };
        candidates.AddRange(DevelopmentCandidates(executable));
        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null)
        {
            _logger.LogInformation("PublisherStudio.MediaHost is not bundled in this development run. Streaming setup remains available, but live sessions require the host process.");
            return;
        }

        try
        {
            _process = Process.Start(new ProcessStartInfo
            {
                FileName = path,
                Arguments = $"--port {settings.MediaHostPort}",
                WorkingDirectory = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (_process is null) return;
            _process.OutputDataReceived += (_, args) => { if (!string.IsNullOrWhiteSpace(args.Data)) _logger.LogInformation("MediaHost: {Line}", args.Data); };
            _process.ErrorDataReceived += (_, args) => { if (!string.IsNullOrWhiteSpace(args.Data)) _logger.LogWarning("MediaHost: {Line}", args.Data); };
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "PublisherStudio.MediaHost could not be started.");
        }
    }


    private static IEnumerable<string> DevelopmentCandidates(string executable)
    {
        var webOutput = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration = webOutput.Parent?.Name;
        var sourceDirectory = webOutput.Parent?.Parent?.Parent?.Parent;
        if (sourceDirectory is null || string.IsNullOrWhiteSpace(configuration)) yield break;

        var hostOutput = Path.Combine(
            sourceDirectory.FullName,
            "PublisherStudio.MediaHost",
            "bin",
            configuration,
            "net10.0");
        yield return Path.Combine(hostOutput, executable);
        yield return Path.Combine(hostOutput, "PublisherStudio.MediaHost");
        yield return Path.Combine(hostOutput, "PublisherStudio.MediaHost.exe");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_process is { HasExited: false }) _process.Kill(entireProcessTree: true);
        }
        catch { }
        return Task.CompletedTask;
    }

    public void Dispose() => _process?.Dispose();
}

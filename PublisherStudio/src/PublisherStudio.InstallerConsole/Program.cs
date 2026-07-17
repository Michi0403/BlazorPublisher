using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

internal static class Program
{
    private const string DefaultRepository = "Michi0403/BlazorPublisher";
    private const string ProductName = "BlazorPublisher";
    private const string SetupFileName = "PublisherStudio.Setup.exe";
    private const string StartMenuGroup = "BlazorPublisher by Michi0403";
    private static readonly HttpClient Http = CreateClient();

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var launchedByDoubleClick = args.Length == 0 && Environment.UserInteractive;
        Options? options = null;
        try
        {
            options = Options.Parse(args);
            if (options.Help)
            {
                Options.PrintHelp();
                return 0;
            }

            return options.Action switch
            {
                SetupAction.Install => await InstallOrUpdateAsync(options, update: false),
                SetupAction.Update => await InstallOrUpdateAsync(options, update: true),
                SetupAction.Start => await StartAsync(options),
                SetupAction.Uninstall => await UninstallAsync(options),
                _ => 0
            };
        }
        catch (Exception ex)
        {
            WriteLine(ex.ToString(), ConsoleColor.Red);
            return 1;
        }
        finally
        {
            if (launchedByDoubleClick || options?.WaitOnExit == true)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to close...");
                Console.ReadKey(intercept: true);
            }
        }
    }

    private static async Task<int> InstallOrUpdateAsync(Options options, bool update)
    {
        var installDirectory = options.InstallDirectory;
        Directory.CreateDirectory(Path.GetDirectoryName(installDirectory)!);
        var tempRoot = Path.Combine(Path.GetTempPath(), $"BlazorPublisher-Setup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            WriteLine(update ? "Updating BlazorPublisher..." : "Installing BlazorPublisher...", ConsoleColor.Cyan);
            var release = await ResolveReleaseAssetAsync(options);
            var archive = Path.Combine(tempRoot, release.AssetName);
            await DownloadFileAsync(release.DownloadUrl, archive);
            var extracted = Path.Combine(tempRoot, "extracted");
            ExtractZipSafe(archive, extracted);
            var payload = ResolvePayloadRoot(extracted);
            ValidatePayload(payload, release.RuntimeIdentifier, release.AssetName);

            StopPublisherProcesses();
            Directory.CreateDirectory(installDirectory);
            ClearApplicationPayload(installDirectory);
            CopyDirectory(payload, installDirectory, overwrite: true);
            CopySetupExecutableIfPossible(installDirectory);
            WriteCommandFiles(installDirectory);
            WriteInstallMetadata(installDirectory, release);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && options.CreateShortcuts)
                ProvisionStartMenu(installDirectory);

            WriteLine($"BlazorPublisher installed to: {installDirectory}", ConsoleColor.Green);
            WriteLine($"Release: {release.TagName} / {release.AssetName}", ConsoleColor.DarkGray);
            return options.StartAfter ? await StartAsync(options) : 0;
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }

    private static async Task<int> StartAsync(Options options)
    {
        var endpointFile = RuntimeEndpointFile();
        if (TryReadEndpoint(endpointFile, out var existingUrl) && IsPublisherRunning())
        {
            WriteLine($"BlazorPublisher is already running. Opening {existingUrl}", ConsoleColor.Yellow);
            OpenBrowser(existingUrl!);
            return 0;
        }

        var launch = ResolveLaunch(options.InstallDirectory);
        try { File.Delete(endpointFile); } catch { }
        var startInfo = new ProcessStartInfo
        {
            FileName = launch.FileName,
            WorkingDirectory = launch.WorkingDirectory,
            UseShellExecute = launch.UseShellExecute
        };
        foreach (var argument in launch.PrefixArguments) startInfo.ArgumentList.Add(argument);
        if (options.Port is int port)
        {
            startInfo.ArgumentList.Add("--port");
            startInfo.ArgumentList.Add(port.ToString());
        }
        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("BlazorPublisher could not be started.");
        WriteLine($"Started BlazorPublisher (PID {process.Id}).", ConsoleColor.Green);
        var url = await WaitForEndpointAsync(endpointFile, process);
        if (!string.IsNullOrWhiteSpace(url))
        {
            WriteLine($"Opening {url}", ConsoleColor.Cyan);
            OpenBrowser(url);
        }
        else WriteLine("The host started, but its endpoint file was not available yet.", ConsoleColor.Yellow);
        return 0;
    }

    private static async Task<int> UninstallAsync(Options options)
    {
        if (!options.Force)
            throw new InvalidOperationException("Use --force to confirm uninstall.");
        StopPublisherProcesses();
        RemoveStartMenu();
        var target = options.InstallDirectory;
        if (!Directory.Exists(target))
        {
            WriteLine("BlazorPublisher is not installed.", ConsoleColor.Yellow);
            return 0;
        }

        var current = Environment.ProcessPath is { Length: > 0 } path ? Path.GetFullPath(path) : string.Empty;
        var targetFull = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (current.StartsWith(targetFull, StringComparison.OrdinalIgnoreCase) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var cleanup = Path.Combine(Path.GetTempPath(), $"BlazorPublisher-Uninstall-{Guid.NewGuid():N}.cmd");
            await File.WriteAllTextAsync(cleanup, $"@echo off\r\nping 127.0.0.1 -n 3 >nul\r\nrmdir /s /q \"{target}\"\r\ndel /q \"%~f0\"\r\n");
            Process.Start(new ProcessStartInfo("cmd.exe", $"/c start \"\" /min \"{cleanup}\"") { UseShellExecute = false, CreateNoWindow = true });
            WriteLine("Uninstall scheduled. The installation folder will be removed after this setup process closes.", ConsoleColor.Green);
        }
        else
        {
            Directory.Delete(target, recursive: true);
            WriteLine($"Removed {target}", ConsoleColor.Green);
        }
        return 0;
    }

    private static async Task<ReleaseAsset> ResolveReleaseAssetAsync(Options options)
    {
        var runtime = CurrentRuntimeIdentifier();
        WriteLine(
            $"Detected runtime: {runtime} (OS architecture: {RuntimeInformation.OSArchitecture}, process architecture: {RuntimeInformation.ProcessArchitecture})",
            ConsoleColor.DarkGray);

        if (!string.IsNullOrWhiteSpace(options.AssetUrl))
        {
            var name = Path.GetFileName(new Uri(options.AssetUrl).AbsolutePath);
            name = string.IsNullOrWhiteSpace(name) ? "BlazorPublisher.zip" : name;
            EnsureAssetNameIsNotIncompatible(name, runtime);
            return new ReleaseAsset("explicit", name, options.AssetUrl, runtime);
        }

        ValidateRepository(options.Repository);

        // Read exactly GitHub's latest release and select the first asset that
        // contains both this repository's platform token and architecture token.
        var latestUrl = $"https://api.github.com/repos/{options.Repository}/releases/latest";
        WriteLine($"Reading latest release: {latestUrl}", ConsoleColor.DarkGray);

        using var stream = await Http.GetStreamAsync(latestUrl).ConfigureAwait(false);
        using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

        var root = json.RootElement;
        var tagName = root.TryGetProperty("tag_name", out var tag)
            ? tag.GetString() ?? "unknown"
            : "unknown";

        WriteLine($"Latest {options.Repository} release: {tagName}", ConsoleColor.DarkGray);

        if (!root.TryGetProperty("assets", out var assets)
            || assets.ValueKind != JsonValueKind.Array
            || assets.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                $"No downloadable release assets found for {options.Repository}.");
        }

        var platform = GetPlatformToken();
        var architecture = GetArchitectureToken();

        if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(architecture))
        {
            throw new PlatformNotSupportedException(
                $"Unsupported platform or architecture: {RuntimeInformation.OSDescription} / {RuntimeInformation.OSArchitecture}.");
        }

        JsonElement? selected = null;

        if (!string.IsNullOrWhiteSpace(options.ReleaseAssetName))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;
                if (name.Equals(options.ReleaseAssetName, StringComparison.OrdinalIgnoreCase))
                {
                    selected = asset;
                    break;
                }
            }

            if (selected is null)
            {
                throw new InvalidOperationException(
                    $"Release {tagName} does not contain the requested asset '{options.ReleaseAssetName}'.");
            }
        }
        else
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;

                var isPlatformMatch =
                    name.Contains(platform, StringComparison.OrdinalIgnoreCase)
                    && name.Contains(architecture, StringComparison.OrdinalIgnoreCase);

                var isSetupAsset =
                    name.Contains("setup", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("installer", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("bootstrap", StringComparison.OrdinalIgnoreCase);

                WriteLine(
                    $"Checking asset '{name}'. PlatformMatch={isPlatformMatch}, SetupAsset={isSetupAsset}, WantedSetupAsset=False",
                    ConsoleColor.DarkGray);

                if (isPlatformMatch && !isSetupAsset)
                {
                    selected = asset;
                    break;
                }
            }

            // Preserve the reference installer's fallback sequence.
            if (selected is null)
            {
                WriteLine(
                    $"No exact asset match found for platform={platform}, architecture={architecture}. Falling back to the first non-setup asset.",
                    ConsoleColor.DarkYellow);

                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? string.Empty;
                    var isSetupAsset =
                        name.Contains("setup", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("installer", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("bootstrap", StringComparison.OrdinalIgnoreCase);

                    if (!isSetupAsset)
                    {
                        selected = asset;
                        break;
                    }
                }
            }

            selected ??= assets.EnumerateArray().First();
        }

        var downloadUrl = selected.Value.GetProperty("browser_download_url").GetString();
        var assetName = selected.Value.GetProperty("name").GetString();

        if (string.IsNullOrWhiteSpace(downloadUrl) || string.IsNullOrWhiteSpace(assetName))
        {
            throw new InvalidOperationException(
                $"Selected release asset for {options.Repository} has no valid name or download URL.");
        }

        EnsureAssetNameIsNotIncompatible(assetName, runtime);

        WriteLine($"Selected asset: {assetName}", ConsoleColor.DarkGray);
        return new ReleaseAsset(tagName, assetName, downloadUrl, runtime);
    }

    private static string GetPlatformToken()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "win";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "lin";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macos";
        return string.Empty;
    }

    private static string GetArchitectureToken() => RuntimeInformation.OSArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.X86 => "x86",
        Architecture.Arm => "arm",
        Architecture.Arm64 => "arm64",
        _ => string.Empty
    };

    private static string CurrentRuntimeIdentifier()
    {
        var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx"
            : throw new PlatformNotSupportedException("BlazorPublisher setup supports Windows, Linux, and macOS.");
        var architecture = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            _ => throw new PlatformNotSupportedException($"Unsupported processor architecture: {RuntimeInformation.OSArchitecture}.")
        };
        return $"{os}-{architecture}";
    }

    private static int ScoreReleaseAsset(string assetName, string runtime)
    {
        var name = NormalizeAssetName(assetName);
        var expected = NormalizeAssetName(runtime);
        if (name.Contains(expected, StringComparison.Ordinal)) return 110;

        var (expectedOs, expectedArchitecture) = SplitRuntime(runtime);
        var osHint = DetectAssetOperatingSystem(name);
        var architectureHint = DetectAssetArchitecture(name);
        if (osHint is null || architectureHint is null) return -1;
        if (!osHint.Equals(expectedOs, StringComparison.Ordinal)) return -1;
        if (!architectureHint.Equals(expectedArchitecture, StringComparison.Ordinal)) return -1;
        return 100;
    }

    private static void EnsureAssetNameIsNotIncompatible(string assetName, string runtime)
    {
        var normalizedName = NormalizeAssetName(assetName);
        var osHint = DetectAssetOperatingSystem(normalizedName);
        var architectureHint = DetectAssetArchitecture(normalizedName);
        var (expectedOs, expectedArchitecture) = SplitRuntime(runtime);
        if (osHint is not null && !osHint.Equals(expectedOs, StringComparison.Ordinal))
            throw new InvalidDataException($"Release asset '{assetName}' targets {osHint}, but this computer requires {runtime}.");
        if (architectureHint is not null && !architectureHint.Equals(expectedArchitecture, StringComparison.Ordinal))
            throw new InvalidDataException($"Release asset '{assetName}' targets {architectureHint}, but this computer requires {runtime}.");
    }

    private static (string OperatingSystem, string Architecture) SplitRuntime(string runtime)
    {
        var separator = runtime.LastIndexOf('-');
        return separator > 0
            ? (runtime[..separator], runtime[(separator + 1)..])
            : (runtime, string.Empty);
    }

    private static string NormalizeAssetName(string value) => new(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string? DetectAssetOperatingSystem(string normalizedName)
    {
        if (normalizedName.Contains("windows", StringComparison.Ordinal)
            || normalizedName.Contains("winx64", StringComparison.Ordinal)
            || normalizedName.Contains("winarm64", StringComparison.Ordinal)
            || normalizedName.StartsWith("win", StringComparison.Ordinal)) return "win";
        if (normalizedName.Contains("linux", StringComparison.Ordinal)
            || normalizedName.Contains("linx64", StringComparison.Ordinal)
            || normalizedName.Contains("linarm64", StringComparison.Ordinal)
            || normalizedName.StartsWith("lin", StringComparison.Ordinal)) return "linux";
        if (normalizedName.Contains("macos", StringComparison.Ordinal)
            || normalizedName.Contains("darwin", StringComparison.Ordinal)
            || normalizedName.Contains("osxx64", StringComparison.Ordinal)
            || normalizedName.Contains("osxarm64", StringComparison.Ordinal)
            || normalizedName.StartsWith("osx", StringComparison.Ordinal)
            || normalizedName.StartsWith("mac", StringComparison.Ordinal)) return "osx";
        return null;
    }

    private static string? DetectAssetArchitecture(string normalizedName)
    {
        if (normalizedName.Contains("arm64", StringComparison.Ordinal) || normalizedName.Contains("aarch64", StringComparison.Ordinal)) return "arm64";
        if (normalizedName.Contains("x64", StringComparison.Ordinal)
            || normalizedName.Contains("amd64", StringComparison.Ordinal)
            || normalizedName.Contains("win64", StringComparison.Ordinal)
            || normalizedName.Contains("linux64", StringComparison.Ordinal)
            || normalizedName.Contains("lin64", StringComparison.Ordinal)
            || normalizedName.Contains("macos64", StringComparison.Ordinal)
            || normalizedName.Contains("osx64", StringComparison.Ordinal)) return "x64";
        if (normalizedName.Contains("x86", StringComparison.Ordinal) || normalizedName.Contains("i386", StringComparison.Ordinal) || normalizedName.Contains("win32", StringComparison.Ordinal)) return "x86";
        return null;
    }

    private static async Task DownloadFileAsync(string url, string destination)
    {
        const int maxAttempts = 3;
        var temporaryFile = destination + ".part";

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(destination));
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                if (File.Exists(temporaryFile))
                    File.Delete(temporaryFile);

                WriteLine($"Downloading attempt {attempt}/{maxAttempts}: {url}", ConsoleColor.Cyan);
                WriteLine($"Target: {destination}", ConsoleColor.DarkGray);

                using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(30));
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("BlazorPublisherSetupTool/1.0");
                request.Headers.Accept.ParseAdd("*/*");

                using var response = await Http.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellation.Token).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                var contentLength = response.Content.Headers.ContentLength;
                WriteLine(
                    contentLength.HasValue
                        ? $"Remote size: {FormatBytes(contentLength.Value)}"
                        : "Remote size: unknown",
                    ConsoleColor.DarkGray);

                long totalRead = 0;

                await using (var input = await response.Content
                    .ReadAsStreamAsync(cancellation.Token)
                    .ConfigureAwait(false))
                await using (var output = new FileStream(
                    temporaryFile,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4 * 1024 * 1024,
                    useAsync: true))
                {
                    var buffer = new byte[4 * 1024 * 1024];
                    var lastReport = DateTimeOffset.UtcNow;

                    while (true)
                    {
                        var read = await input
                            .ReadAsync(buffer.AsMemory(0, buffer.Length), cancellation.Token)
                            .ConfigureAwait(false);

                        if (read == 0)
                            break;

                        await output
                            .WriteAsync(buffer.AsMemory(0, read), cancellation.Token)
                            .ConfigureAwait(false);

                        totalRead += read;

                        var now = DateTimeOffset.UtcNow;
                        if (now - lastReport >= TimeSpan.FromSeconds(5))
                        {
                            var progress = contentLength is > 0
                                ? $"Downloaded {FormatBytes(totalRead)} / {FormatBytes(contentLength.Value)} ({totalRead * 100.0 / contentLength.Value:F1}%)"
                                : $"Downloaded {FormatBytes(totalRead)}";

                            WriteLine(progress, ConsoleColor.DarkGray);
                            lastReport = now;
                        }
                    }

                    await output.FlushAsync(cancellation.Token).ConfigureAwait(false);
                }

                if (!File.Exists(temporaryFile))
                {
                    throw new FileNotFoundException(
                        $"Temporary download file does not exist after download: {temporaryFile}");
                }

                var actualSize = new FileInfo(temporaryFile).Length;
                if (actualSize == 0)
                    throw new IOException("Downloaded file is empty.");

                if (contentLength.HasValue && actualSize != contentLength.Value)
                {
                    var missing = contentLength.Value - actualSize;
                    throw new IOException(
                        $"Incomplete download. Got {actualSize:N0} bytes, expected {contentLength.Value:N0} bytes. Missing {missing:N0} bytes.");
                }

                await MoveFileWithRetryAsync(temporaryFile, destination).ConfigureAwait(false);
                WriteLine($"Download complete: {destination} ({FormatBytes(actualSize)})", ConsoleColor.Green);
                return;
            }
            catch (Exception ex)
            {
                WriteLine(
                    $"Download attempt {attempt}/{maxAttempts} failed: {ex.Message}",
                    ConsoleColor.Yellow);

                try
                {
                    if (File.Exists(temporaryFile))
                        File.Delete(temporaryFile);
                }
                catch
                {
                    // Best-effort cleanup.
                }

                if (attempt == maxAttempts)
                    throw;

                await Task.Delay(TimeSpan.FromSeconds(2 * attempt)).ConfigureAwait(false);
            }
        }
    }

    private static async Task MoveFileWithRetryAsync(string source, string destination)
    {
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                if (!File.Exists(source))
                    throw new FileNotFoundException($"Source file for move does not exist: {source}", source);

                if (File.Exists(destination))
                    File.Delete(destination);

                File.Move(source, destination, overwrite: true);
                return;
            }
            catch (IOException ex) when (attempt < 10)
            {
                WriteLine(
                    $"Move failed because the file is locked. Retry {attempt}/10: {ex.Message}",
                    ConsoleColor.Yellow);
                await Task.Delay(300).ConfigureAwait(false);
            }
        }

        if (File.Exists(destination))
            File.Delete(destination);

        File.Move(source, destination, overwrite: true);
    }

    private static void ExtractZipSafe(string archive, string destination)
    {
        Directory.CreateDirectory(destination);
        var root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        using var zip = ZipFile.OpenRead(archive);
        foreach (var entry in zip.Entries)
        {
            var target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Unsafe ZIP entry: {entry.FullName}");
            if (string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(target); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    private static string ResolvePayloadRoot(string extracted)
    {
        var executable = Directory.EnumerateFiles(extracted, "PublisherStudio.Web.exe", SearchOption.AllDirectories).FirstOrDefault();
        var dll = executable is null ? Directory.EnumerateFiles(extracted, "PublisherStudio.Web.dll", SearchOption.AllDirectories).FirstOrDefault() : null;
        var marker = executable ?? dll ?? throw new FileNotFoundException("The release ZIP does not contain PublisherStudio.Web.exe or PublisherStudio.Web.dll.");
        return Path.GetDirectoryName(marker)!;
    }

    private static void ValidatePayload(string payload, string runtime, string assetName)
    {
        var runtimeConfig = Path.Combine(payload, "PublisherStudio.Web.runtimeconfig.json");
        var dependencies = Path.Combine(payload, "PublisherStudio.Web.deps.json");
        var applicationDll = Path.Combine(payload, "PublisherStudio.Web.dll");
        var missing = new List<string>();
        if (!File.Exists(runtimeConfig)) missing.Add(Path.GetFileName(runtimeConfig));
        if (!File.Exists(dependencies)) missing.Add(Path.GetFileName(dependencies));
        if (!File.Exists(applicationDll)) missing.Add(Path.GetFileName(applicationDll));
        if (missing.Count > 0)
            throw new InvalidDataException($"Release asset '{assetName}' is not a complete BlazorPublisher publish for {runtime}. Missing: {string.Join(", ", missing)}.");

        var payloadRuntime = ReadPayloadRuntimeIdentifier(dependencies);
        if (!string.IsNullOrWhiteSpace(payloadRuntime) && ScoreReleaseAsset(payloadRuntime, runtime) < 0)
            throw new InvalidDataException(
                $"Release asset '{assetName}' contains a {payloadRuntime} publish, but this computer requires {runtime}. " +
                "The existing installation was not changed.");

        using var document = JsonDocument.Parse(File.ReadAllText(runtimeConfig));
        if (!document.RootElement.TryGetProperty("runtimeOptions", out var runtimeOptions))
            throw new InvalidDataException($"Release asset '{assetName}' has an invalid PublisherStudio.Web.runtimeconfig.json.");
        var frameworkDependent = runtimeOptions.TryGetProperty("framework", out _)
            || runtimeOptions.TryGetProperty("frameworks", out _);
        if (frameworkDependent) return;

        var nativeFiles = runtime.StartsWith("win-", StringComparison.Ordinal)
            ? new[] { "PublisherStudio.Web.exe", "hostfxr.dll", "hostpolicy.dll" }
            : runtime.StartsWith("linux-", StringComparison.Ordinal)
                ? new[] { "PublisherStudio.Web", "libhostfxr.so", "libhostpolicy.so" }
                : new[] { "PublisherStudio.Web", "libhostfxr.dylib", "libhostpolicy.dylib" };
        missing = nativeFiles.Where(file => !File.Exists(Path.Combine(payload, file))).ToList();
        if (missing.Count > 0)
            throw new InvalidDataException(
                $"Release asset '{assetName}' claims to be self-contained but is incomplete for {runtime}. Missing: {string.Join(", ", missing)}. " +
                $"Rebuild it with Build-Release.ps1 -Runtime {runtime}; the existing installation was not changed.");
    }

    private static string? ReadPayloadRuntimeIdentifier(string dependenciesFile)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(dependenciesFile));
            if (!document.RootElement.TryGetProperty("runtimeTarget", out var runtimeTarget)
                || !runtimeTarget.TryGetProperty("name", out var nameValue)) return null;
            var name = nameValue.GetString();
            if (string.IsNullOrWhiteSpace(name)) return null;
            var separator = name.LastIndexOf('/');
            var candidate = separator >= 0 && separator < name.Length - 1 ? name[(separator + 1)..] : name;
            var normalized = NormalizeAssetName(candidate);
            return DetectAssetOperatingSystem(normalized) is not null && DetectAssetArchitecture(normalized) is not null
                ? candidate
                : null;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("PublisherStudio.Web.deps.json is invalid.", ex);
        }
    }

    private static void ClearApplicationPayload(string target)
    {
        var preserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            SetupFileName, "Install.cmd", "Update.cmd", "Start.cmd", "Uninstall.cmd", "installation.json"
        };
        foreach (var file in Directory.EnumerateFiles(target))
            if (!preserved.Contains(Path.GetFileName(file))) TryDeleteFile(file);
        foreach (var directory in Directory.EnumerateDirectories(target))
            try { Directory.Delete(directory, recursive: true); } catch (Exception ex) { WriteLine($"Could not remove old directory '{directory}': {ex.Message}", ConsoleColor.Yellow); }
    }

    private static void CopySetupExecutableIfPossible(string installDirectory)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        var source = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(source) || !File.Exists(source)) return;
        var destination = Path.Combine(installDirectory, SetupFileName);
        if (Path.GetFullPath(source).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase)) return;
        File.Copy(source, destination, overwrite: true);
    }

    private static void WriteCommandFiles(string installDirectory)
    {
        var setup = Path.Combine(installDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? SetupFileName : Path.GetFileName(Environment.ProcessPath ?? "PublisherStudio.Setup"));
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        WriteCommand(Path.Combine(installDirectory, "Install.cmd"), $"\"{setup}\" --install --start");
        WriteCommand(Path.Combine(installDirectory, "Update.cmd"), $"\"{setup}\" --update --start");
        WriteCommand(Path.Combine(installDirectory, "Start.cmd"), $"\"{setup}\" --start");
        WriteCommand(Path.Combine(installDirectory, "Uninstall.cmd"), $"\"{setup}\" --uninstall --force");
    }

    private static void WriteCommand(string path, string command)
    {
        File.WriteAllText(path, $"@echo off\r\nsetlocal\r\ncd /d \"%~dp0\"\r\ncall {command}\r\nset \"EXITCODE=%ERRORLEVEL%\"\r\nif not \"%EXITCODE%\"==\"0\" echo Command failed with exit code %EXITCODE%.\r\nif not \"%EXITCODE%\"==\"0\" pause\r\nexit /b %EXITCODE%\r\n", new UTF8Encoding(false));
    }

    private static void ProvisionStartMenu(string installDirectory)
    {
        var folder = StartMenuFolder();
        Directory.CreateDirectory(folder);
        var productIcon = FirstExistingPath(
            Path.Combine(installDirectory, "PublisherStudio.ico"),
            Path.Combine(installDirectory, "PublisherStudio.Web.exe"),
            Path.Combine(installDirectory, SetupFileName));
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Start.url"), Path.Combine(installDirectory, "Start.cmd"), productIcon);
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Update.url"), Path.Combine(installDirectory, "Update.cmd"), productIcon);
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Install or Repair.url"), Path.Combine(installDirectory, "Install.cmd"), productIcon);
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Uninstall.url"), Path.Combine(installDirectory, "Uninstall.cmd"), productIcon);
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Folder.url"), installDirectory, productIcon);
        WriteLine($"Start Menu entries created in '{StartMenuGroup}'.", ConsoleColor.Green);
    }

    private static string? FirstExistingPath(params string?[] paths) => paths.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));

    private static void CreateUrlShortcut(string shortcutPath, string targetPath, string? iconPath)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[InternetShortcut]");
        builder.AppendLine($"URL={new Uri(Path.GetFullPath(targetPath)).AbsoluteUri}");
        if (!string.IsNullOrWhiteSpace(iconPath)) { builder.AppendLine($"IconFile={Path.GetFullPath(iconPath)}"); builder.AppendLine("IconIndex=0"); }
        File.WriteAllText(shortcutPath, builder.ToString(), new UTF8Encoding(false));
    }

    private static void RemoveStartMenu()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        var folder = StartMenuFolder();
        if (Directory.Exists(folder)) try { Directory.Delete(folder, recursive: true); } catch { }
    }

    private static string StartMenuFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", StartMenuGroup);

    private static LaunchInfo ResolveLaunch(string installDirectory)
    {
        var executable = Directory.Exists(installDirectory)
            ? Directory.EnumerateFiles(installDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "PublisherStudio.Web.exe" : "PublisherStudio.Web", SearchOption.AllDirectories).FirstOrDefault()
            : null;
        if (!string.IsNullOrWhiteSpace(executable)) return new LaunchInfo(executable, Path.GetDirectoryName(executable)!, true, []);
        var dll = Directory.Exists(installDirectory) ? Directory.EnumerateFiles(installDirectory, "PublisherStudio.Web.dll", SearchOption.AllDirectories).FirstOrDefault() : null;
        if (!string.IsNullOrWhiteSpace(dll)) return new LaunchInfo("dotnet", Path.GetDirectoryName(dll)!, false, [dll]);
        throw new FileNotFoundException($"BlazorPublisher is not installed in '{installDirectory}'. Run Install first.");
    }

    private static async Task<string?> WaitForEndpointAsync(string endpointFile, Process process)
    {
        for (var attempt = 0; attempt < 120; attempt++)
        {
            if (process.HasExited) throw new InvalidOperationException($"BlazorPublisher exited with code {process.ExitCode}.");
            if (TryReadEndpoint(endpointFile, out var url)) return url;
            await Task.Delay(125);
        }
        return null;
    }

    private static bool TryReadEndpoint(string file, out string? url)
    {
        url = null;
        if (!File.Exists(file)) return false;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            if (document.RootElement.TryGetProperty("BaseUrl", out var value)) url = value.GetString();
            return !string.IsNullOrWhiteSpace(url);
        }
        catch { return false; }
    }

    private static void StopPublisherProcesses()
    {
        foreach (var process in Process.GetProcessesByName("PublisherStudio.Web"))
        {
            try
            {
                WriteLine($"Stopping PublisherStudio.Web PID {process.Id}...", ConsoleColor.Yellow);
                if (!process.CloseMainWindow() || !process.WaitForExit(2500)) process.Kill(entireProcessTree: true);
            }
            catch { }
            finally { process.Dispose(); }
        }
    }

    private static bool IsPublisherRunning() => Process.GetProcessesByName("PublisherStudio.Web").Any();
    private static string RuntimeEndpointFile() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PublisherStudio", "runtime", "server.json");
    private static void OpenBrowser(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private static void CopyDirectory(string source, string target, bool overwrite)
    {
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite);
        }
    }

    private static void WriteInstallMetadata(string target, ReleaseAsset release)
    {
        var metadata = new { Product = ProductName, InstalledUtc = DateTimeOffset.UtcNow, release.TagName, release.AssetName, release.RuntimeIdentifier, InstallerVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() };
        File.WriteAllText(Path.Combine(target, "installation.json"), JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void ValidateRepository(string repository)
    {
        if (string.IsNullOrWhiteSpace(repository) || repository.Count(character => character == '/') != 1)
            throw new ArgumentException("Repository must use owner/name format.");
    }

    private static void TryDeleteFile(string path) { try { File.SetAttributes(path, FileAttributes.Normal); File.Delete(path); } catch (Exception ex) { WriteLine($"Could not remove old file '{path}': {ex.Message}", ConsoleColor.Yellow); } }
    private static string FormatBytes(long value) { string[] units = ["B", "KB", "MB", "GB"]; double size = value; var index = 0; while (size >= 1024 && index < units.Length - 1) { size /= 1024; index++; } return $"{size:0.##} {units[index]}"; }
    private static HttpClient CreateClient() { var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) }; client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BlazorPublisher-Setup", "1.0")); client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json")); return client; }
    private static void WriteLine(string value, ConsoleColor color) { var previous = Console.ForegroundColor; Console.ForegroundColor = color; Console.WriteLine(value); Console.ForegroundColor = previous; }

    private sealed record ReleaseAsset(string TagName, string AssetName, string DownloadUrl, string RuntimeIdentifier);
    private sealed record LaunchInfo(string FileName, string WorkingDirectory, bool UseShellExecute, string[] PrefixArguments);
    private enum SetupAction { Install, Update, Start, Uninstall, Help }

    private sealed class Options
    {
        public SetupAction Action { get; private set; } = SetupAction.Install;
        public string Repository { get; private set; } = DefaultRepository;
        public string? AssetUrl { get; private set; }
        public string? ReleaseAssetName { get; private set; }
        public string InstallDirectory { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "BlazorPublisher");
        public int? Port { get; private set; }
        public bool StartAfter { get; private set; } = true;
        public bool CreateShortcuts { get; private set; } = true;
        public bool Force { get; private set; }
        public bool WaitOnExit { get; private set; }
        public bool Help { get; private set; }

        public static Options Parse(string[] args)
        {
            var result = new Options();
            if (args.Length == 0) return result;
            var actionSpecified = false;
            for (var index = 0; index < args.Length; index++)
            {
                var argument = args[index].Trim();
                string Next() => ++index < args.Length ? args[index] : throw new ArgumentException($"Missing value after {argument}.");
                switch (argument.ToLowerInvariant())
                {
                    case "install" or "--install": result.Action = SetupAction.Install; result.StartAfter = true; actionSpecified = true; break;
                    case "update" or "--update": result.Action = SetupAction.Update; result.StartAfter = true; actionSpecified = true; break;
                    case "start": result.Action = SetupAction.Start; result.StartAfter = false; actionSpecified = true; break;
                    case "--start":
                        if (actionSpecified && result.Action is SetupAction.Install or SetupAction.Update) result.StartAfter = true;
                        else { result.Action = SetupAction.Start; result.StartAfter = false; actionSpecified = true; }
                        break;
                    case "uninstall" or "--uninstall": result.Action = SetupAction.Uninstall; result.StartAfter = false; actionSpecified = true; break;
                    case "--repo": result.Repository = Next(); break;
                    case "--asset-url": result.AssetUrl = Next(); break;
                    case "--release-asset": result.ReleaseAssetName = Next(); break;
                    case "--install-dir": result.InstallDirectory = Path.GetFullPath(Next()); break;
                    case "--port": result.Port = int.Parse(Next()); break;
                    case "--no-start": result.StartAfter = false; break;
                    case "--no-shortcuts": result.CreateShortcuts = false; break;
                    case "--force": result.Force = true; break;
                    case "--wait" or "--pause": result.WaitOnExit = true; break;
                    case "--help" or "-h" or "/?": result.Help = true; result.Action = SetupAction.Help; break;
                    default: throw new ArgumentException($"Unknown option: {argument}");
                }
            }
            return result;
        }

        public static void PrintHelp() => Console.WriteLine("""
BlazorPublisher setup

Double-click without arguments:
  Downloads the latest Michi0403/BlazorPublisher release, installs it, creates Start Menu entries, starts the server, and opens the browser.

Commands:
  PublisherStudio.Setup.exe --install [--start]
  PublisherStudio.Setup.exe --update [--start]
  PublisherStudio.Setup.exe --start [--port 5198]
  PublisherStudio.Setup.exe --uninstall --force

Options:
  --repo owner/name          GitHub repository. Default: Michi0403/BlazorPublisher
  --asset-url URL            Direct application ZIP URL instead of GitHub latest release
  --release-asset NAME       Select an exact release asset name
  --install-dir PATH         Installation folder
  --no-start                 Install/update without starting
  --no-shortcuts             Do not create Start Menu entries
  --wait                     Wait for a key before closing
""");
    }
}
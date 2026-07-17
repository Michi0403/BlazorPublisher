using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

internal static class Program
{
    private const string Repository = "Michi0403/BlazorPublisher";
    private const string ProductName = "BlazorPublisher";
    private const string SetupFileName = "PublisherStudio.Setup.exe";
    private const string IconFileName = "PublisherStudio.ico";
    private const string ApplicationFolderName = "Application";
    private const string SetupFolderName = "Setup";
    private const string StartMenuGroup = "BlazorPublisher by Michi0403";
    private static readonly HttpClient Http = CreateHttpClient();

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
        var installRoot = options.InstallDirectory;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"BlazorPublisher-Setup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            WriteLine(update ? "Updating BlazorPublisher..." : "Installing BlazorPublisher...", ConsoleColor.Cyan);

            var applicationRelease = await ResolveLatestReleaseAssetAsync(options, setupAsset: false);
            var setupRelease = await ResolveLatestReleaseAssetAsync(options, setupAsset: true);

            var applicationArchive = Path.Combine(tempRoot, applicationRelease.AssetName);
            var setupDownload = Path.Combine(tempRoot, SetupFileName);
            await DownloadFileAsync(applicationRelease.DownloadUrl, applicationArchive);
            await DownloadFileAsync(setupRelease.DownloadUrl, setupDownload);
            ValidateSetupExecutable(setupDownload, setupRelease.AssetName);

            var extracted = Path.Combine(tempRoot, "extracted");
            ExtractZipSafe(applicationArchive, extracted);
            var payload = ResolvePayloadRoot(extracted);
            ValidatePayload(payload, applicationRelease.RuntimeIdentifier, applicationRelease.AssetName);

            StopPublisherProcesses();
            Directory.CreateDirectory(installRoot);
            ReplaceApplicationDirectory(payload, ApplicationDirectory(installRoot));
            InstallSetupExecutable(setupDownload, SetupDirectory(installRoot));
            RemoveLegacyFlatLayout(installRoot);
            InstallProductIcon(installRoot);
            WriteCommandFiles(installRoot);
            WriteInstallMetadata(installRoot, applicationRelease, setupRelease);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && options.CreateShortcuts)
                ProvisionStartMenu(installRoot);

            WriteLine($"BlazorPublisher installed to: {installRoot}", ConsoleColor.Green);
            WriteLine($"Application: {ApplicationDirectory(installRoot)}", ConsoleColor.DarkGray);
            WriteLine($"Setup: {SetupDirectory(installRoot)}", ConsoleColor.DarkGray);
            WriteLine($"Release: {applicationRelease.TagName} / {applicationRelease.AssetName}", ConsoleColor.DarkGray);

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
        foreach (var argument in launch.PrefixArguments)
            startInfo.ArgumentList.Add(argument);
        if (options.Port is int port)
        {
            startInfo.ArgumentList.Add("--port");
            startInfo.ArgumentList.Add(port.ToString());
        }

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("BlazorPublisher could not be started.");
        WriteLine($"Started BlazorPublisher (PID {process.Id}).", ConsoleColor.Green);

        var url = await WaitForEndpointAsync(endpointFile, process);
        if (!string.IsNullOrWhiteSpace(url))
        {
            WriteLine($"Opening {url}", ConsoleColor.Cyan);
            OpenBrowser(url);
        }
        else
        {
            WriteLine("The host started, but its endpoint file was not available yet.", ConsoleColor.Yellow);
        }

        return 0;
    }

    private static async Task<int> UninstallAsync(Options options)
    {
        if (!options.Force)
            throw new InvalidOperationException("Use --force to confirm uninstall.");

        StopPublisherProcesses();
        RemoveStartMenu();

        var installRoot = options.InstallDirectory;
        if (!Directory.Exists(installRoot))
        {
            WriteLine("BlazorPublisher is not installed.", ConsoleColor.Yellow);
            return 0;
        }

        if (IsCurrentCodeInside(installRoot) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var cleanup = Path.Combine(Path.GetTempPath(), $"BlazorPublisher-Uninstall-{Guid.NewGuid():N}.cmd");
            await File.WriteAllTextAsync(cleanup,
                $"@echo off\r\nping 127.0.0.1 -n 3 >nul\r\nrmdir /s /q \"{installRoot}\"\r\ndel /q \"%~f0\"\r\n",
                new UTF8Encoding(false));
            Process.Start(new ProcessStartInfo("cmd.exe", $"/c start \"\" /min \"{cleanup}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
            WriteLine("Uninstall scheduled. The installation folder will be removed after this setup process closes.", ConsoleColor.Green);
        }
        else
        {
            Directory.Delete(installRoot, recursive: true);
            WriteLine($"Removed {installRoot}", ConsoleColor.Green);
        }

        return 0;
    }

    private static async Task<ReleaseAsset> ResolveLatestReleaseAssetAsync(Options options, bool setupAsset)
    {
        var runtime = CurrentRuntimeIdentifier();
        var explicitUrl = setupAsset ? options.SetupAssetUrl : options.AssetUrl;
        var explicitName = setupAsset ? options.SetupAssetName : options.ReleaseAssetName;

        if (!string.IsNullOrWhiteSpace(explicitUrl))
        {
            var name = Path.GetFileName(new Uri(explicitUrl).AbsolutePath);
            if (string.IsNullOrWhiteSpace(name))
                name = setupAsset ? SetupFileName : ExpectedApplicationNames(runtime)[0];
            if (!setupAsset)
                EnsureApplicationAssetCompatible(name, runtime);
            return new ReleaseAsset("explicit", name, explicitUrl, runtime);
        }

        var latestUrl = $"https://api.github.com/repos/{Repository}/releases/latest";
        WriteLine($"Reading latest release: {latestUrl}", ConsoleColor.DarkGray);
        using var response = await Http.GetAsync(latestUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

        var root = json.RootElement;
        var tagName = root.TryGetProperty("tag_name", out var tag) ? tag.GetString() ?? "unknown" : "unknown";
        WriteLine($"Latest {Repository} release: {tagName}", ConsoleColor.DarkGray);

        if (!root.TryGetProperty("assets", out var assets) || assets.GetArrayLength() == 0)
            throw new InvalidOperationException($"No downloadable release assets found for {Repository}.");

        JsonElement? selected = null;
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? string.Empty;
            var exactNameMatch = !string.IsNullOrWhiteSpace(explicitName)
                && name.Equals(explicitName, StringComparison.OrdinalIgnoreCase);
            var match = setupAsset
                ? IsRuntimeSpecificSetupAsset(name, runtime)
                : IsApplicationAsset(name, runtime);

            WriteLine($"Checking asset '{name}'. Match={match}, SetupAsset={setupAsset}", ConsoleColor.DarkGray);
            if (exactNameMatch || (string.IsNullOrWhiteSpace(explicitName) && match))
            {
                selected = asset;
                break;
            }
        }

        if (selected is null && setupAsset && string.IsNullOrWhiteSpace(explicitName))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;
                if (name.Equals(SetupFileName, StringComparison.OrdinalIgnoreCase))
                {
                    selected = asset;
                    break;
                }
            }
        }

        if (selected is null)
        {
            var expected = setupAsset
                ? $"'{SetupFileName}' or a runtime-specific setup executable"
                : string.Join(", ", ExpectedApplicationNames(runtime));
            throw new InvalidOperationException($"Latest release {tagName} does not contain {expected}.");
        }

        var assetName = selected.Value.GetProperty("name").GetString() ?? string.Empty;
        var downloadUrl = selected.Value.GetProperty("browser_download_url").GetString();
        if (string.IsNullOrWhiteSpace(downloadUrl))
            throw new InvalidOperationException($"Selected release asset '{assetName}' has no download URL.");
        if (!setupAsset)
            EnsureApplicationAssetCompatible(assetName, runtime);

        WriteLine($"Selected asset: {assetName}", ConsoleColor.Green);
        return new ReleaseAsset(tagName, assetName, downloadUrl, runtime);
    }

    private static bool IsApplicationAsset(string assetName, string runtime)
    {
        if (!assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || IsSetupName(assetName))
            return false;
        var normalized = NormalizeName(assetName);
        return RuntimeTokens(runtime).Any(token => normalized.Contains(token, StringComparison.Ordinal));
    }

    private static bool IsRuntimeSpecificSetupAsset(string assetName, string runtime)
    {
        if (assetName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) || !IsSetupName(assetName))
            return false;
        var normalized = NormalizeName(assetName);
        return RuntimeTokens(runtime).Any(token => normalized.Contains(token, StringComparison.Ordinal));
    }

    private static bool IsSetupName(string name)
    {
        var normalized = NormalizeName(name);
        return normalized.Contains("setup", StringComparison.Ordinal)
            || normalized.Contains("installer", StringComparison.Ordinal)
            || normalized.Contains("bootstrap", StringComparison.Ordinal);
    }

    private static void EnsureApplicationAssetCompatible(string assetName, string runtime)
    {
        if (!IsApplicationAsset(assetName, runtime))
            throw new InvalidDataException(
                $"Release asset '{assetName}' does not match {runtime}. Expected: {string.Join(", ", ExpectedApplicationNames(runtime))}.");
    }

    private static string[] RuntimeTokens(string runtime) => runtime switch
    {
        "win-x64" => ["winx64", "windowsx64"],
        "win-arm64" => ["winarm64", "windowsarm64"],
        "linux-x64" => ["linx64", "linuxx64"],
        "linux-arm64" => ["linarm64", "linuxarm64"],
        "osx-x64" => ["macosx64", "osxx64", "darwinx64"],
        "osx-arm64" => ["macosarm64", "osxarm64", "darwinarm64"],
        _ => throw new PlatformNotSupportedException($"Unsupported runtime: {runtime}")
    };

    private static string[] ExpectedApplicationNames(string runtime) => runtime switch
    {
        "win-x64" => ["winx64.zip", "BlazorPublisher-win-x64.zip"],
        "win-arm64" => ["winarm64.zip", "BlazorPublisher-win-arm64.zip"],
        "linux-x64" => ["linx64.zip", "linuxx64.zip", "BlazorPublisher-linux-x64.zip"],
        "linux-arm64" => ["linarm64.zip", "linuxarm64.zip", "BlazorPublisher-linux-arm64.zip"],
        "osx-x64" => ["macosx64.zip", "osxx64.zip", "BlazorPublisher-osx-x64.zip"],
        "osx-arm64" => ["macosarm64.zip", "osxarm64.zip", "BlazorPublisher-osx-arm64.zip"],
        _ => throw new PlatformNotSupportedException($"Unsupported runtime: {runtime}")
    };

    private static string CurrentRuntimeIdentifier()
    {
        var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win"
            : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx"
            : throw new PlatformNotSupportedException("BlazorPublisher supports Windows, Linux, and macOS.");
        var architecture = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException($"Unsupported architecture: {RuntimeInformation.OSArchitecture}.")
        };
        var runtime = $"{platform}-{architecture}";
        WriteLine($"Detected runtime: {runtime}", ConsoleColor.DarkGray);
        return runtime;
    }

    private static async Task DownloadFileAsync(string url, string destination)
    {
        const int maxAttempts = 3;
        var temporary = destination + ".part";

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
                TryDeleteFile(temporary);
                WriteLine($"Downloading attempt {attempt}/{maxAttempts}: {url}", ConsoleColor.Cyan);

                using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(30));
                using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellation.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var expectedLength = response.Content.Headers.ContentLength;
                long total = 0;

                await using (var input = await response.Content.ReadAsStreamAsync(cancellation.Token).ConfigureAwait(false))
                await using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4 * 1024 * 1024, true))
                {
                    var buffer = new byte[4 * 1024 * 1024];
                    while (true)
                    {
                        var read = await input.ReadAsync(buffer.AsMemory(), cancellation.Token).ConfigureAwait(false);
                        if (read == 0) break;
                        await output.WriteAsync(buffer.AsMemory(0, read), cancellation.Token).ConfigureAwait(false);
                        total += read;
                        if (expectedLength is > 0)
                            Console.Write($"\r{total * 100d / expectedLength.Value:0.0}%   ");
                    }
                }
                Console.WriteLine();

                if (total == 0)
                    throw new IOException("Downloaded file is empty.");
                if (expectedLength.HasValue && total != expectedLength.Value)
                    throw new IOException($"Incomplete download: {total:N0} of {expectedLength.Value:N0} bytes.");

                await MoveFileWithRetryAsync(temporary, destination).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                WriteLine($"Download attempt {attempt} failed: {ex.Message}", ConsoleColor.Yellow);
                TryDeleteFile(temporary);
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
                if (File.Exists(destination)) File.Delete(destination);
                File.Move(source, destination);
                return;
            }
            catch (IOException) when (attempt < 10)
            {
                await Task.Delay(300).ConfigureAwait(false);
            }
        }
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
            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(target);
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    private static string ResolvePayloadRoot(string extracted)
    {
        var marker = Directory.EnumerateFiles(extracted, "PublisherStudio.Web.exe", SearchOption.AllDirectories).FirstOrDefault()
            ?? Directory.EnumerateFiles(extracted, "PublisherStudio.Web", SearchOption.AllDirectories).FirstOrDefault()
            ?? Directory.EnumerateFiles(extracted, "PublisherStudio.Web.dll", SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new FileNotFoundException("The release ZIP does not contain PublisherStudio.Web.");
        return Path.GetDirectoryName(marker)!;
    }

    private static void ValidatePayload(string payload, string runtime, string assetName)
    {
        var required = new List<string>
        {
            "PublisherStudio.Web.dll",
            "PublisherStudio.Web.deps.json",
            "PublisherStudio.Web.runtimeconfig.json"
        };
        var missing = required.Where(file => !File.Exists(Path.Combine(payload, file))).ToList();
        if (missing.Count > 0)
            throw new InvalidDataException($"'{assetName}' is incomplete. Missing: {string.Join(", ", missing)}.");

        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(payload, "PublisherStudio.Web.runtimeconfig.json")));
        var runtimeOptions = document.RootElement.GetProperty("runtimeOptions");
        var frameworkDependent = runtimeOptions.TryGetProperty("framework", out _)
            || runtimeOptions.TryGetProperty("frameworks", out _);
        if (frameworkDependent) return;

        var native = runtime.StartsWith("win-", StringComparison.Ordinal)
            ? new[] { "PublisherStudio.Web.exe", "hostfxr.dll", "hostpolicy.dll" }
            : runtime.StartsWith("linux-", StringComparison.Ordinal)
                ? new[] { "PublisherStudio.Web", "libhostfxr.so", "libhostpolicy.so" }
                : new[] { "PublisherStudio.Web", "libhostfxr.dylib", "libhostpolicy.dylib" };
        missing = native.Where(file => !File.Exists(Path.Combine(payload, file))).ToList();
        if (missing.Count > 0)
            throw new InvalidDataException($"'{assetName}' is not a complete self-contained {runtime} publish. Missing: {string.Join(", ", missing)}.");
    }

    private static void ValidateSetupExecutable(string path, string assetName)
    {
        if (!File.Exists(path) || new FileInfo(path).Length < 64 * 1024)
            throw new InvalidDataException($"Setup asset '{assetName}' is missing or incomplete.");
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        using var stream = File.OpenRead(path);
        if (stream.ReadByte() != 'M' || stream.ReadByte() != 'Z')
            throw new InvalidDataException($"Setup asset '{assetName}' is not a Windows executable.");
    }

    private static void ReplaceApplicationDirectory(string payload, string destination)
    {
        var incoming = destination + ".incoming";
        var backup = destination + ".backup";
        TryDeleteDirectory(incoming);
        TryDeleteDirectory(backup);
        CopyDirectory(payload, incoming);

        try
        {
            if (Directory.Exists(destination)) Directory.Move(destination, backup);
            Directory.Move(incoming, destination);
            TryDeleteDirectory(backup);
        }
        catch
        {
            TryDeleteDirectory(destination);
            if (Directory.Exists(backup)) Directory.Move(backup, destination);
            throw;
        }
    }

    private static void InstallSetupExecutable(string source, string setupDirectory)
    {
        Directory.CreateDirectory(setupDirectory);
        var destination = Path.Combine(setupDirectory, SetupFileName);
        if (!CurrentCodeLocations().Any(path => PathsEqual(path, destination)))
        {
            File.Copy(source, destination, overwrite: true);
            return;
        }

        var next = destination + ".next";
        File.Copy(source, next, overwrite: true);
        var script = Path.Combine(Path.GetTempPath(), $"BlazorPublisher-Setup-Replace-{Guid.NewGuid():N}.cmd");
        File.WriteAllText(script,
            $"@echo off\r\nping 127.0.0.1 -n 3 >nul\r\nmove /y \"{next}\" \"{destination}\" >nul\r\ndel /q \"%~f0\"\r\n",
            new UTF8Encoding(false));
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c start \"\" /min \"{script}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    private static void RemoveLegacyFlatLayout(string installRoot)
    {
        var keptFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Install.cmd", "Update.cmd", "Start.cmd", "Uninstall.cmd", "installation.json"
        };
        var keptDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ApplicationFolderName, SetupFolderName
        };

        foreach (var file in Directory.EnumerateFiles(installRoot))
            if (!keptFiles.Contains(Path.GetFileName(file))) TryDeleteFile(file);
        foreach (var directory in Directory.EnumerateDirectories(installRoot))
            if (!keptDirectories.Contains(Path.GetFileName(directory))) TryDeleteDirectory(directory);
    }

    private static void InstallProductIcon(string installRoot)
    {
        var source = Directory.EnumerateFiles(ApplicationDirectory(installRoot), IconFileName, SearchOption.AllDirectories)
            .OrderBy(path => RelativeDepth(ApplicationDirectory(installRoot), path))
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(source))
            File.Copy(source, Path.Combine(installRoot, IconFileName), overwrite: true);
    }

    private static void WriteCommandFiles(string installRoot)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        WriteCommand(Path.Combine(installRoot, "Install.cmd"), "--install --start");
        WriteCommand(Path.Combine(installRoot, "Update.cmd"), "--update --start");
        WriteCommand(Path.Combine(installRoot, "Start.cmd"), "--start");
        WriteCommand(Path.Combine(installRoot, "Uninstall.cmd"), "--uninstall --force");
    }

    private static void WriteCommand(string path, string arguments)
    {
        var setup = $"%~dp0{SetupFolderName}\\{SetupFileName}";
        File.WriteAllText(path,
            $"@echo off\r\nsetlocal\r\ncd /d \"%~dp0\"\r\ncall \"{setup}\" {arguments} --install-dir \"%~dp0\"\r\n" +
            "set \"EXITCODE=%ERRORLEVEL%\"\r\nif not \"%EXITCODE%\"==\"0\" echo Command failed with exit code %EXITCODE%.\r\n" +
            "if not \"%EXITCODE%\"==\"0\" pause\r\nexit /b %EXITCODE%\r\n",
            new UTF8Encoding(false));
    }

    private static void ProvisionStartMenu(string installRoot)
    {
        var folder = StartMenuFolder();
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        Directory.CreateDirectory(folder);
        var icon = File.Exists(Path.Combine(installRoot, IconFileName))
            ? Path.Combine(installRoot, IconFileName)
            : Path.Combine(SetupDirectory(installRoot), SetupFileName);

        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Start.url"), Path.Combine(installRoot, "Start.cmd"), icon);
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Update.url"), Path.Combine(installRoot, "Update.cmd"), icon);
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Install or Repair.url"), Path.Combine(installRoot, "Install.cmd"), icon);
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Uninstall.url"), Path.Combine(installRoot, "Uninstall.cmd"), icon);
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Folder.url"), installRoot, icon);
        WriteLine($"Start Menu entries created in '{StartMenuGroup}'.", ConsoleColor.Green);
    }

    private static void CreateUrlShortcut(string shortcutPath, string targetPath, string iconPath)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[InternetShortcut]");
        builder.AppendLine($"URL={new Uri(Path.GetFullPath(targetPath)).AbsoluteUri}");
        if (File.Exists(iconPath))
        {
            builder.AppendLine($"IconFile={Path.GetFullPath(iconPath)}");
            builder.AppendLine("IconIndex=0");
        }
        File.WriteAllText(shortcutPath, builder.ToString(), new UTF8Encoding(false));
    }

    private static LaunchInfo ResolveLaunch(string installRoot)
    {
        var appRoot = ApplicationDirectory(installRoot);
        if (!Directory.Exists(appRoot))
            throw new DirectoryNotFoundException($"Application directory not found: {appRoot}");

        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "PublisherStudio.Web.exe" : "PublisherStudio.Web";
        var executable = Directory.EnumerateFiles(appRoot, executableName, SearchOption.AllDirectories)
            .OrderBy(path => RelativeDepth(appRoot, path)).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(executable))
            return new LaunchInfo(executable, Path.GetDirectoryName(executable)!, true, []);

        var dll = Directory.EnumerateFiles(appRoot, "PublisherStudio.Web.dll", SearchOption.AllDirectories)
            .OrderBy(path => RelativeDepth(appRoot, path)).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(dll))
            return new LaunchInfo("dotnet", Path.GetDirectoryName(dll)!, false, [dll]);

        throw new FileNotFoundException($"PublisherStudio.Web was not found under '{appRoot}'.");
    }

    private static async Task<string?> WaitForEndpointAsync(string endpointFile, Process process)
    {
        for (var attempt = 0; attempt < 160; attempt++)
        {
            if (process.HasExited)
                throw new InvalidOperationException($"BlazorPublisher exited with code {process.ExitCode}.");
            if (TryReadEndpoint(endpointFile, out var url)) return url;
            await Task.Delay(125).ConfigureAwait(false);
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
                if (!process.CloseMainWindow() || !process.WaitForExit(2500)) process.Kill(entireProcessTree: true);
            }
            catch { }
            finally { process.Dispose(); }
        }
    }

    private static bool IsPublisherRunning()
    {
        var processes = Process.GetProcessesByName("PublisherStudio.Web");
        try { return processes.Length > 0; }
        finally { foreach (var process in processes) process.Dispose(); }
    }

    private static IEnumerable<string> CurrentCodeLocations()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath) && File.Exists(Environment.ProcessPath))
            yield return Environment.ProcessPath;
        var assembly = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrWhiteSpace(assembly) && File.Exists(assembly)) yield return assembly;
        var command = Environment.GetCommandLineArgs().FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(command) && File.Exists(command)) yield return command;
    }

    private static bool IsCurrentCodeInside(string root)
    {
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return CurrentCodeLocations().Any(path => Path.GetFullPath(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private static void WriteInstallMetadata(string installRoot, ReleaseAsset application, ReleaseAsset setup)
    {
        var metadata = new
        {
            Product = ProductName,
            InstalledUtc = DateTimeOffset.UtcNow,
            application.TagName,
            ApplicationAsset = application.AssetName,
            SetupAsset = setup.AssetName,
            application.RuntimeIdentifier,
            InstallerVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        };
        File.WriteAllText(Path.Combine(installRoot, "installation.json"),
            JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string ApplicationDirectory(string root) => Path.Combine(root, ApplicationFolderName);
    private static string SetupDirectory(string root) => Path.Combine(root, SetupFolderName);
    private static string StartMenuFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", StartMenuGroup);
    private static string RuntimeEndpointFile() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PublisherStudio", "runtime", "server.json");
    private static string NormalizeName(string value) => new(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    private static bool PathsEqual(string left, string right) => Path.GetFullPath(left).Equals(Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
    private static int RelativeDepth(string root, string path) => Path.GetRelativePath(root, path).Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar);
    private static void OpenBrowser(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private static void RemoveStartMenu()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        try { Directory.Delete(StartMenuFolder(), recursive: true); } catch { }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return;
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }
        catch { }
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch { }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BlazorPublisher-Setup", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    private static void WriteLine(string value, ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(value);
        Console.ForegroundColor = previous;
    }

    private sealed record ReleaseAsset(string TagName, string AssetName, string DownloadUrl, string RuntimeIdentifier);
    private sealed record LaunchInfo(string FileName, string WorkingDirectory, bool UseShellExecute, string[] PrefixArguments);
    private enum SetupAction { Install, Update, Start, Uninstall, Help }

    private sealed class Options
    {
        public SetupAction Action { get; private set; } = SetupAction.Install;
        public string? AssetUrl { get; private set; }
        public string? SetupAssetUrl { get; private set; }
        public string? ReleaseAssetName { get; private set; }
        public string? SetupAssetName { get; private set; }
        public string InstallDirectory { get; private set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "BlazorPublisher");
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
                    case "--asset-url": result.AssetUrl = Next(); break;
                    case "--setup-url": result.SetupAssetUrl = Next(); break;
                    case "--release-asset": result.ReleaseAssetName = Next(); break;
                    case "--setup-asset": result.SetupAssetName = Next(); break;
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

            if (result.Port.HasValue && (result.Port.Value <= 0 || result.Port.Value > 65535))
                throw new ArgumentOutOfRangeException(nameof(result.Port), "Port must be between 1 and 65535.");
            return result;
        }

        public static void PrintHelp() => Console.WriteLine("""
BlazorPublisher setup

Commands:
  PublisherStudio.Setup.exe --install [--start]
  PublisherStudio.Setup.exe --update [--start]
  PublisherStudio.Setup.exe --start [--port 5198]
  PublisherStudio.Setup.exe --uninstall --force

The application is installed into Application\ and the installer into Setup\.
The generated Install.cmd, Update.cmd, Start.cmd, and Uninstall.cmd always call Setup\PublisherStudio.Setup.exe.
""");
    }
}

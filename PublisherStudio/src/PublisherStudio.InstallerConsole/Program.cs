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

            StopPublisherProcesses();
            Directory.CreateDirectory(installDirectory);
            ClearApplicationPayload(installDirectory);
            CopyDirectory(payload, installDirectory, overwrite: true);
            CopySetupExecutableIfPossible(installDirectory);
            WriteCommandFiles(installDirectory, options);
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
        if (!string.IsNullOrWhiteSpace(options.AssetUrl))
        {
            var name = Path.GetFileName(new Uri(options.AssetUrl).AbsolutePath);
            return new ReleaseAsset("explicit", string.IsNullOrWhiteSpace(name) ? "BlazorPublisher.zip" : name, options.AssetUrl);
        }

        ValidateRepository(options.Repository);
        var api = $"https://api.github.com/repos/{options.Repository}/releases?per_page=30";
        WriteLine($"Reading published releases: {api}", ConsoleColor.DarkGray);
        using var response = await Http.GetAsync(api, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        if (json.RootElement.ValueKind != JsonValueKind.Array || json.RootElement.GetArrayLength() == 0)
            throw new InvalidOperationException("No published GitHub release exists yet. Create a release and upload the application ZIP plus the setup EXE.");

        var runtime = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64";
        foreach (var release in json.RootElement.EnumerateArray())
        {
            if (release.TryGetProperty("draft", out var draft) && draft.GetBoolean()) continue;
            var tag = release.TryGetProperty("tag_name", out var tagValue) ? tagValue.GetString() ?? "release" : "release";
            if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) continue;

            var candidates = assets.EnumerateArray().Select(asset => new
            {
                Name = asset.GetProperty("name").GetString() ?? string.Empty,
                Url = asset.GetProperty("browser_download_url").GetString() ?? string.Empty
            }).Where(asset => asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                && !asset.Name.Contains("source", StringComparison.OrdinalIgnoreCase)
                && !asset.Name.Contains("installer", StringComparison.OrdinalIgnoreCase)
                && !asset.Name.Contains("setup", StringComparison.OrdinalIgnoreCase)).ToList();

            var selected = !string.IsNullOrWhiteSpace(options.ReleaseAssetName)
                ? candidates.FirstOrDefault(asset => asset.Name.Equals(options.ReleaseAssetName, StringComparison.OrdinalIgnoreCase))
                : candidates.FirstOrDefault(asset => asset.Name.Contains(runtime, StringComparison.OrdinalIgnoreCase))
                    ?? candidates.FirstOrDefault(asset => asset.Name.Contains("win", StringComparison.OrdinalIgnoreCase))
                    ?? candidates.FirstOrDefault();
            if (selected is not null && !string.IsNullOrWhiteSpace(selected.Url))
            {
                var prerelease = release.TryGetProperty("prerelease", out var prereleaseValue) && prereleaseValue.GetBoolean();
                WriteLine($"Selected {(prerelease ? "pre-release" : "release")} {tag}: {selected.Name}", ConsoleColor.DarkGray);
                return new ReleaseAsset(tag, selected.Name, selected.Url);
            }
        }

        throw new InvalidOperationException($"No application ZIP was found in the published releases. Expected an asset such as BlazorPublisher-{runtime}.zip.");
    }

    private static async Task DownloadFileAsync(string url, string destination)
    {
        WriteLine($"Downloading {url}", ConsoleColor.Cyan);
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var length = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync();
        await using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, useAsync: true);
        var buffer = new byte[1024 * 1024];
        long total = 0;
        var nextReport = 0L;
        while (true)
        {
            var read = await input.ReadAsync(buffer);
            if (read == 0) break;
            await output.WriteAsync(buffer.AsMemory(0, read));
            total += read;
            if (total >= nextReport)
            {
                var text = length is > 0 ? $"{total * 100d / length.Value:0.0}%" : FormatBytes(total);
                Console.Write($"\r{text,-12}");
                nextReport = total + 4L * 1024 * 1024;
            }
        }
        Console.WriteLine();
        if (total == 0) throw new IOException("The downloaded release asset is empty.");
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

    private static void WriteCommandFiles(string installDirectory, Options options)
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
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Start.url"), Path.Combine(installDirectory, "Start.cmd"));
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Update.url"), Path.Combine(installDirectory, "Update.cmd"));
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Install or Repair.url"), Path.Combine(installDirectory, "Install.cmd"));
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Uninstall.url"), Path.Combine(installDirectory, "Uninstall.cmd"));
        CreateUrlShortcut(Path.Combine(folder, "BlazorPublisher Folder.url"), installDirectory);
        WriteLine($"Start Menu entries created in '{StartMenuGroup}'.", ConsoleColor.Green);
    }

    private static void CreateUrlShortcut(string shortcutPath, string targetPath)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[InternetShortcut]");
        builder.AppendLine($"URL={new Uri(Path.GetFullPath(targetPath)).AbsoluteUri}");
        var icon = Directory.EnumerateFiles(Path.GetDirectoryName(targetPath) ?? string.Empty, "*.ico", SearchOption.AllDirectories).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(icon)) { builder.AppendLine($"IconFile={icon}"); builder.AppendLine("IconIndex=0"); }
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
        var metadata = new { Product = ProductName, InstalledUtc = DateTimeOffset.UtcNow, release.TagName, release.AssetName, InstallerVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() };
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

    private sealed record ReleaseAsset(string TagName, string AssetName, string DownloadUrl);
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

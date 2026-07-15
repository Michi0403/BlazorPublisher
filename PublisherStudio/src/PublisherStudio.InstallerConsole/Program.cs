using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static class Program
{
    private static readonly HttpClient Http = CreateClient();

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        try
        {
            var options = Options.Parse(args);
            if (options.Help || string.IsNullOrWhiteSpace(options.Command)) { Options.PrintHelp(); return 0; }
            return options.Command switch
            {
                "install" => await InstallAsync(options),
                "start" => await StartAsync(options),
                "uninstall" => Uninstall(options),
                "source" => await BuildFromSourceZipAsync(options),
                _ => throw new ArgumentException($"Unknown command: {options.Command}")
            };
        }
        catch (Exception ex)
        {
            WriteLine(ex.Message, ConsoleColor.Red);
            return 1;
        }
    }

    private static async Task<int> InstallAsync(Options options)
    {
        var payload = RequireDirectory(options.Payload, "--payload");
        var target = options.InstallDirectory;
        WriteLine($"Installing PublisherStudio to {target}", ConsoleColor.Cyan);
        if (Directory.Exists(target) && options.Force) Directory.Delete(target, true);
        Directory.CreateDirectory(target);
        CopyDirectory(payload, target, overwrite: true);
        WriteInstallMetadata(target);
        WriteLine("Installation complete.", ConsoleColor.Green);
        return options.StartAfter ? await StartAsync(options) : 0;
    }

    private static async Task<int> StartAsync(Options options)
    {
        var target = options.InstallDirectory;
        var executable = ResolveWebExecutable(target);
        var arguments = options.Port is null ? string.Empty : $"--port {options.Port}";
        var endpointFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PublisherStudio", "runtime", "server.json");
        try { File.Delete(endpointFile); } catch { }
        var process = Process.Start(new ProcessStartInfo(executable, arguments)
        {
            WorkingDirectory = target,
            UseShellExecute = true
        }) ?? throw new InvalidOperationException("The PublisherStudio process could not be started.");
        WriteLine($"Started PublisherStudio (PID {process.Id}).", ConsoleColor.Green);

        var baseUrl = await WaitForEndpointAsync(endpointFile, process);
        if (baseUrl is not null)
        {
            WriteLine($"Opening {baseUrl}", ConsoleColor.Cyan);
            Process.Start(new ProcessStartInfo(baseUrl) { UseShellExecute = true });
        }
        else
        {
            WriteLine("The host started, but no runtime endpoint file appeared. Open the URL shown by PublisherStudio.Web.", ConsoleColor.Yellow);
        }
        return 0;
    }

    private static async Task<string?> WaitForEndpointAsync(string endpointFile, Process process)
    {
        for (var attempt = 0; attempt < 80; attempt++)
        {
            if (process.HasExited) throw new InvalidOperationException($"PublisherStudio exited with code {process.ExitCode} before publishing its endpoint.");
            if (File.Exists(endpointFile))
            {
                try
                {
                    using var document = JsonDocument.Parse(await File.ReadAllTextAsync(endpointFile));
                    if (document.RootElement.TryGetProperty("BaseUrl", out var property)) return property.GetString();
                }
                catch (IOException) { }
                catch (JsonException) { }
            }
            await Task.Delay(125);
        }
        return null;
    }

    private static int Uninstall(Options options)
    {
        var target = options.InstallDirectory;
        if (!Directory.Exists(target)) { WriteLine("PublisherStudio is not installed in the selected directory.", ConsoleColor.Yellow); return 0; }
        if (!options.Force) throw new InvalidOperationException("Uninstall requires --force to prevent accidental deletion.");
        Directory.Delete(target, recursive: true);
        WriteLine($"Removed {target}", ConsoleColor.Green);
        return 0;
    }

    private static async Task<int> BuildFromSourceZipAsync(Options options)
    {
        if (string.IsNullOrWhiteSpace(options.SourceZip)) throw new ArgumentException("source requires --source-zip <URL-or-file>.");
        var temp = Path.Combine(Path.GetTempPath(), "PublisherStudio-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var zipPath = Path.Combine(temp, "source.zip");
            if (Uri.TryCreate(options.SourceZip, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                WriteLine($"Downloading {uri}", ConsoleColor.Cyan);
                await using var destination = File.Create(zipPath);
                await using var source = await Http.GetStreamAsync(uri);
                await source.CopyToAsync(destination);
            }
            else File.Copy(Path.GetFullPath(options.SourceZip), zipPath);
            ZipFile.ExtractToDirectory(zipPath, Path.Combine(temp, "source"));
            var project = Directory.EnumerateFiles(Path.Combine(temp, "source"), "PublisherStudio.Web.csproj", SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new FileNotFoundException("PublisherStudio.Web.csproj was not found in the source ZIP.");
            var output = Path.Combine(temp, "payload");
            var runtime = options.RuntimeIdentifier ?? DefaultRuntimeIdentifier();
            await RunDotNetAsync($"publish \"{project}\" -c Release -r {runtime} --self-contained false -o \"{output}\"");
            options.Payload = output;
            return await InstallAsync(options);
        }
        finally { try { Directory.Delete(temp, true); } catch { } }
    }

    private static async Task RunDotNetAsync(string arguments)
    {
        WriteLine("dotnet " + arguments, ConsoleColor.DarkGray);
        using var process = Process.Start(new ProcessStartInfo("dotnet", arguments)
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
        }) ?? throw new InvalidOperationException("dotnet could not be started.");
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) Console.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) Console.Error.WriteLine(e.Data); };
        process.BeginOutputReadLine(); process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException($"dotnet publish failed with exit code {process.ExitCode}.");
    }

    private static string ResolveWebExecutable(string directory)
    {
        var names = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[] { "PublisherStudio.Web.exe", "PublisherStudio.Web" }
            : new[] { "PublisherStudio.Web" };
        foreach (var name in names)
        {
            var path = Path.Combine(directory, name);
            if (File.Exists(path)) return path;
        }
        var dll = Path.Combine(directory, "PublisherStudio.Web.dll");
        if (File.Exists(dll)) return CreateDotNetLauncher(directory, dll);
        throw new FileNotFoundException("No published PublisherStudio.Web executable was found.");
    }

    private static string CreateDotNetLauncher(string directory, string dll)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var cmd = Path.Combine(directory, "PublisherStudio.Web.cmd");
            File.WriteAllText(cmd, $"@echo off\r\ndotnet \"{dll}\" %*\r\n");
            return cmd;
        }
        var sh = Path.Combine(directory, "PublisherStudio.Web.sh");
        File.WriteAllText(sh, $"#!/usr/bin/env sh\nexec dotnet \"{dll}\" \"$@\"\n");
        try { File.SetUnixFileMode(sh, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute); } catch { }
        return sh;
    }

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

    private static string RequireDirectory(string? value, string option)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{option} is required.");
        var path = Path.GetFullPath(value);
        return Directory.Exists(path) ? path : throw new DirectoryNotFoundException(path);
    }
    private static void WriteInstallMetadata(string target) => File.WriteAllText(Path.Combine(target, "installation.json"), JsonSerializer.Serialize(new { InstalledUtc = DateTimeOffset.UtcNow, Version = "0.1.0" }, new JsonSerializerOptions { WriteIndented = true }));
    private static string DefaultRuntimeIdentifier() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64") : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64") : (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64");
    private static HttpClient CreateClient() { var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) }; client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PublisherStudio-Setup", "0.1")); return client; }
    private static void WriteLine(string value, ConsoleColor color) { var previous = Console.ForegroundColor; Console.ForegroundColor = color; Console.WriteLine(value); Console.ForegroundColor = previous; }

    private sealed class Options
    {
        public string? Command { get; private set; }
        public string? Payload { get; set; }
        public string? SourceZip { get; private set; }
        public string? RuntimeIdentifier { get; private set; }
        public int? Port { get; private set; }
        public bool StartAfter { get; private set; }
        public bool Force { get; private set; }
        public bool Help { get; private set; }
        public string InstallDirectory { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "PublisherStudio");

        public static Options Parse(string[] args)
        {
            var result = new Options();
            if (args.Length > 0 && !args[0].StartsWith('-')) result.Command = args[0].ToLowerInvariant();
            for (var i = result.Command is null ? 0 : 1; i < args.Length; i++)
            {
                string Next() => ++i < args.Length ? args[i] : throw new ArgumentException($"Missing value after {args[i - 1]}.");
                switch (args[i])
                {
                    case "--payload": result.Payload = Next(); break;
                    case "--source-zip": result.SourceZip = Next(); break;
                    case "--install-dir": result.InstallDirectory = Path.GetFullPath(Next()); break;
                    case "--runtime": result.RuntimeIdentifier = Next(); break;
                    case "--port": result.Port = int.Parse(Next()); break;
                    case "--start": result.StartAfter = true; break;
                    case "--force": result.Force = true; break;
                    case "--help" or "-h": result.Help = true; break;
                    default: throw new ArgumentException($"Unknown option: {args[i]}");
                }
            }
            return result;
        }
        public static void PrintHelp() => Console.WriteLine("""
PublisherStudio.Setup

  install   --payload <published-folder> [--install-dir <folder>] [--force] [--start] [--port N]
  start     [--install-dir <folder>] [--port N]
  uninstall [--install-dir <folder>] --force
  source    --source-zip <URL-or-file> [--runtime win-x64] [--install-dir <folder>] [--force] [--start]

The source command downloads/extracts a ZIP and runs dotnet publish. Git is not required.
""");
    }
}

using System.Diagnostics;

namespace PublisherStudio.Backend.Streaming.Encoding;

public static class FfmpegLocator
{
    public static string? Resolve(string? configuredPath = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var expanded = Environment.ExpandEnvironmentVariables(configuredPath.Trim().Trim('"'));
            if (File.Exists(expanded)) return Path.GetFullPath(expanded);
            if (TryResolveCommand(expanded, out var configuredCommand)) return configuredCommand;
            return null;
        }

        var bundledNames = OperatingSystem.IsWindows()
            ? new[] { "ffmpeg.exe", Path.Combine("tools", "ffmpeg", "ffmpeg.exe"), Path.Combine("tools", "ffmpeg.exe") }
            : new[] { "ffmpeg", Path.Combine("tools", "ffmpeg", "ffmpeg"), Path.Combine("tools", "ffmpeg") };
        foreach (var name in bundledNames)
        {
            var candidate = Path.Combine(AppContext.BaseDirectory, name);
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
        }

        foreach (var candidate in KnownInstallLocations())
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);

        return TryResolveCommand(OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg", out var command)
            ? command
            : null;
    }

    public static bool IsAvailable(string? configuredPath = null) => Resolve(configuredPath) is not null;

    public static async Task<string?> ReadVersionAsync(string? configuredPath = null, CancellationToken cancellationToken = default)
    {
        var executable = Resolve(configuredPath);
        if (executable is null) return null;
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ArgumentList = { "-version" }
            });
            if (process is null) return null;
            var firstLine = await process.StandardOutput.ReadLineAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(firstLine) ? null : firstLine.Trim();
        }
        catch { return null; }
    }

    private static IEnumerable<string> KnownInstallLocations()
    {
        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var chocolatey = Environment.GetEnvironmentVariable("ChocolateyInstall");
            if (!string.IsNullOrWhiteSpace(local))
            {
                yield return Path.Combine(local, "Microsoft", "WinGet", "Links", "ffmpeg.exe");
                foreach (var candidate in FindWinGetPackageExecutables(local))
                    yield return candidate;
            }
            if (!string.IsNullOrWhiteSpace(profile))
                yield return Path.Combine(profile, "scoop", "shims", "ffmpeg.exe");
            if (!string.IsNullOrWhiteSpace(chocolatey))
                yield return Path.Combine(chocolatey, "bin", "ffmpeg.exe");
            yield break;
        }

        yield return "/usr/local/bin/ffmpeg";
        yield return "/usr/bin/ffmpeg";
        yield return "/opt/homebrew/bin/ffmpeg";
        yield return "/opt/local/bin/ffmpeg";
        yield return "/snap/bin/ffmpeg";
    }


    private static IEnumerable<string> FindWinGetPackageExecutables(string localAppData)
    {
        var packagesRoot = Path.Combine(localAppData, "Microsoft", "WinGet", "Packages");
        if (!Directory.Exists(packagesRoot)) yield break;

        IEnumerable<string> packageDirectories;
        try
        {
            packageDirectories = Directory.EnumerateDirectories(packagesRoot, "Gyan.FFmpeg*", SearchOption.TopDirectoryOnly).ToArray();
        }
        catch
        {
            yield break;
        }

        foreach (var packageDirectory in packageDirectories)
        {
            IEnumerable<string> matches;
            try
            {
                matches = Directory.EnumerateFiles(packageDirectory, "ffmpeg.exe", SearchOption.AllDirectories)
                    .OrderByDescending(path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .Take(4)
                    .ToArray();
            }
            catch
            {
                continue;
            }

            foreach (var match in matches)
                yield return match;
        }
    }

    private static bool TryResolveCommand(string command, out string path)
    {
        path = string.Empty;
        if (Path.IsPathRooted(command) || command.Contains(Path.DirectorySeparatorChar) || command.Contains(Path.AltDirectorySeparatorChar))
        {
            if (!File.Exists(command)) return false;
            path = Path.GetFullPath(command);
            return true;
        }

        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : new[] { string.Empty };
        var hasExtension = Path.HasExtension(command);
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var extension in hasExtension ? new[] { string.Empty } : extensions)
            {
                var candidate = Path.Combine(directory.Trim('"'), command + extension);
                if (!File.Exists(candidate)) continue;
                path = candidate;
                return true;
            }
        }
        return false;
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

internal static class FfmpegProvisioner
{
    public static string? FindExecutable()
    {
        var executable = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(directory.Trim('"'), executable);
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
        }
        foreach (var candidate in KnownInstallLocations())
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
        return null;
    }

    private static IEnumerable<string> KnownInstallLocations()
    {
        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var chocolatey = Environment.GetEnvironmentVariable("ChocolateyInstall");
            if (!string.IsNullOrWhiteSpace(local))
                yield return Path.Combine(local, "Microsoft", "WinGet", "Links", "ffmpeg.exe");
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

    public static async Task<bool> EnsureInstalledAsync(ILogger logger, CancellationToken cancellationToken = default)
    {
        var existing = FindExecutable();
        if (existing is not null)
        {
            logger.LogInformation("FFmpeg is available at '{Path}'.", existing);
            return true;
        }

        logger.LogWarning("FFmpeg was not found. PublisherStudio will now try the package managers available on this operating system.");
        foreach (var command in InstallationCommands())
        {
            if (!CommandExists(command.FileName)) continue;
            logger.LogInformation("Installing FFmpeg with {Manager}...", command.DisplayName);
            var exitCode = await RunAsync(command, logger, cancellationToken).ConfigureAwait(false);
            if (exitCode != 0)
            {
                logger.LogWarning("{Manager} returned exit code {ExitCode}; trying the next supported package manager.", command.DisplayName, exitCode);
                continue;
            }

            existing = FindExecutable();
            if (existing is not null)
            {
                logger.LogInformation("FFmpeg installation completed: '{Path}'.", existing);
                return true;
            }

            logger.LogWarning("{Manager} completed, but FFmpeg is not visible on the current PATH yet. A new terminal or sign-in may be required.", command.DisplayName);
        }

        logger.LogError("FFmpeg could not be installed automatically. Install FFmpeg with your operating-system package manager, then run PublisherStudio.Setup --check-ffmpeg or configure the executable path in Streaming Studio.");
        return false;
    }

    public static bool ReportStatus(ILogger logger)
    {
        var executable = FindExecutable();
        if (executable is null)
        {
            logger.LogWarning("FFmpeg is not available in the application folder, known package-manager locations, or PATH.");
            return false;
        }
        logger.LogInformation("FFmpeg is available at '{Path}'.", executable);
        return true;
    }

    private static IEnumerable<InstallCommand> InstallationCommands()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return new("winget", ["install", "--id", "Gyan.FFmpeg", "--exact", "--silent", "--accept-package-agreements", "--accept-source-agreements"], "WinGet (Gyan.FFmpeg)");
            yield return new("choco", ["install", "ffmpeg", "-y"], "Chocolatey");
            yield return new("scoop", ["install", "ffmpeg"], "Scoop");
            yield break;
        }

        if (OperatingSystem.IsMacOS())
        {
            yield return new("brew", ["install", "ffmpeg"], "Homebrew");
            yield return new("port", ["install", "ffmpeg"], "MacPorts");
            yield break;
        }

        if (!OperatingSystem.IsLinux()) yield break;
        var elevated = !string.Equals(Environment.UserName, "root", StringComparison.OrdinalIgnoreCase) && CommandExists("sudo");
        foreach (var command in new[]
        {
            new InstallCommand("apt-get", ["update"], "APT update"),
            new InstallCommand("apt-get", ["install", "-y", "ffmpeg"], "APT"),
            new InstallCommand("dnf", ["install", "-y", "ffmpeg"], "DNF"),
            new InstallCommand("yum", ["install", "-y", "ffmpeg"], "YUM"),
            new InstallCommand("zypper", ["--non-interactive", "install", "ffmpeg"], "Zypper"),
            new InstallCommand("pacman", ["--noconfirm", "-S", "ffmpeg"], "Pacman"),
            new InstallCommand("apk", ["add", "ffmpeg"], "APK")
        })
        {
            if (!elevated) yield return command;
            else yield return new InstallCommand("sudo", [command.FileName, .. command.Arguments], $"sudo {command.DisplayName}");
        }
    }

    private static bool CommandExists(string command)
    {
        if (Path.IsPathRooted(command)) return File.Exists(command);
        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM").Split(';', StringSplitOptions.RemoveEmptyEntries)
            : new[] { string.Empty };
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        foreach (var extension in Path.HasExtension(command) ? new[] { string.Empty } : extensions)
            if (File.Exists(Path.Combine(directory.Trim('"'), command + extension))) return true;
        return false;
    }

    private static async Task<int> RunAsync(InstallCommand command, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command.FileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (var argument in command.Arguments) startInfo.ArgumentList.Add(argument);
            using var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (_, eventArgs) => { if (!string.IsNullOrWhiteSpace(eventArgs.Data)) logger.LogInformation("{Line}", eventArgs.Data); };
            process.ErrorDataReceived += (_, eventArgs) => { if (!string.IsNullOrWhiteSpace(eventArgs.Data)) logger.LogWarning("{Line}", eventArgs.Data); };
            if (!process.Start()) return -1;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not run {Manager}.", command.DisplayName);
            return -1;
        }
    }

    private sealed record InstallCommand(string FileName, string[] Arguments, string DisplayName);
}

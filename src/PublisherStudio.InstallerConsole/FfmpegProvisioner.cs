using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace PublisherStudio.InstallerConsole;
/// <summary>
/// Represents a FFmpeg provisioner application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
internal static class FfmpegProvisioner
{
    /// <summary>
    /// Stores the shared read-only progress heartbeat value used by <see cref="FfmpegProvisioner"/> across instances of the containing type.
    /// </summary>
    private static readonly TimeSpan ProgressHeartbeat = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Finds executable for <see cref="FfmpegProvisioner"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg provisioner workflow.
    /// </summary>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Performs known install locations for <see cref="FfmpegProvisioner"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg provisioner workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Finds win get package executables for <see cref="FfmpegProvisioner"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg provisioner workflow.
    /// </summary>
    /// <param name="localAppData">Local app data value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
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

    /// <summary>
    /// Ensures installed for <see cref="FfmpegProvisioner"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg provisioner workflow.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public static async Task<bool> EnsureInstalledAsync(ILogger logger, CancellationToken cancellationToken = default)
    {
        var existing = FindExecutable();
        if (existing is not null && await IsRunnableAsync(existing, cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("FFmpeg is available at '{Path}'.", existing);
            return true;
        }

        logger.LogWarning("FFmpeg was not found. PublisherStudio will try the available package managers.");
        logger.LogInformation("FFmpeg provisioning is optional. A failed or timed-out download will not invalidate the PublisherStudio installation.");

        var anyManagerFound = false;
        var provisioningDeadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(15);
        foreach (var command in InstallationCommands())
        {
            if (!CommandExists(command.FileName)) continue;
            anyManagerFound = true;

            for (var attempt = 1; attempt <= command.MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var remainingBudget = provisioningDeadline - DateTimeOffset.UtcNow;
                if (remainingBudget <= TimeSpan.Zero)
                {
                    logger.LogWarning("The 15-minute FFmpeg provisioning budget was exhausted. PublisherStudio setup will continue.");
                    goto ProvisioningFinished;
                }

                var boundedCommand = command with { Timeout = command.Timeout < remainingBudget ? command.Timeout : remainingBudget };
                logger.LogInformation(
                    "Installing FFmpeg with {Manager} (attempt {Attempt}/{Attempts}, time limit {Minutes} minutes; total provisioning budget 15 minutes)...",
                    command.DisplayName,
                    attempt,
                    command.MaxAttempts,
                    Math.Ceiling(boundedCommand.Timeout.TotalMinutes));

                var result = await RunAsync(boundedCommand, logger, cancellationToken).ConfigureAwait(false);

                // Some package managers return a source warning/non-zero code after the package itself was installed.
                existing = FindExecutable();
                if (existing is not null && await IsRunnableAsync(existing, cancellationToken).ConfigureAwait(false))
                {
                    logger.LogInformation("FFmpeg installation completed: '{Path}'.", existing);
                    return true;
                }

                if (result.TimedOut)
                    logger.LogWarning("{Manager} exceeded its time limit and was stopped. The installer will continue instead of waiting forever.", command.DisplayName);
                else if (result.ExitCode != 0)
                    logger.LogWarning("{Manager} returned exit code {ExitCode}.", command.DisplayName, result.ExitCode);
                else
                    logger.LogWarning("{Manager} completed, but FFmpeg is not visible yet.", command.DisplayName);

                if (attempt < command.MaxAttempts)
                {
                    var retryDelay = TimeSpan.FromSeconds(5 * attempt);
                    logger.LogInformation("Retrying {Manager} in {Seconds} seconds. Package-manager caches will be reused when available.", command.DisplayName, retryDelay.TotalSeconds);
                    await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

ProvisioningFinished:
        if (!anyManagerFound)
            logger.LogWarning("No supported package manager was found for automatic FFmpeg installation.");

        logger.LogWarning("FFmpeg could not be provisioned automatically. PublisherStudio itself remains installed and usable; streaming features can be enabled later with PublisherStudio.Setup --install-ffmpeg or by configuring an FFmpeg executable in Streaming Studio.");
        return false;
    }

    /// <summary>
    /// Performs report status for <see cref="FfmpegProvisioner"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg provisioner workflow.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Performs installation commands for <see cref="FfmpegProvisioner"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg provisioner workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private static IEnumerable<InstallCommand> InstallationCommands()
    {
        if (OperatingSystem.IsWindows())
        {
            // Pinning the source avoids an unrelated Microsoft Store source refresh delaying or failing this install.
            yield return new(
                "winget",
                ["install", "--id", "Gyan.FFmpeg", "--exact", "--source", "winget", "--silent", "--disable-interactivity", "--accept-package-agreements", "--accept-source-agreements"],
                "WinGet (Gyan.FFmpeg)",
                TimeSpan.FromMinutes(12),
                2);
            yield return new("choco", ["install", "ffmpeg", "-y", "--limit-output"], "Chocolatey", TimeSpan.FromMinutes(20), 1);
            yield return new("scoop", ["install", "ffmpeg"], "Scoop", TimeSpan.FromMinutes(20), 1);
            yield break;
        }

        if (OperatingSystem.IsMacOS())
        {
            yield return new("brew", ["install", "ffmpeg"], "Homebrew", TimeSpan.FromMinutes(25), 1);
            yield return new("port", ["install", "ffmpeg"], "MacPorts", TimeSpan.FromMinutes(25), 1);
            yield break;
        }

        if (!OperatingSystem.IsLinux()) yield break;
        var elevated = !string.Equals(Environment.UserName, "root", StringComparison.OrdinalIgnoreCase) && CommandExists("sudo");
        foreach (var command in new[]
        {
            new InstallCommand("apt-get", ["update"], "APT update", TimeSpan.FromMinutes(10), 1),
            new InstallCommand("apt-get", ["install", "-y", "ffmpeg"], "APT", TimeSpan.FromMinutes(20), 1),
            new InstallCommand("dnf", ["install", "-y", "ffmpeg"], "DNF", TimeSpan.FromMinutes(20), 1),
            new InstallCommand("yum", ["install", "-y", "ffmpeg"], "YUM", TimeSpan.FromMinutes(20), 1),
            new InstallCommand("zypper", ["--non-interactive", "install", "ffmpeg"], "Zypper", TimeSpan.FromMinutes(20), 1),
            new InstallCommand("pacman", ["--noconfirm", "-S", "ffmpeg"], "Pacman", TimeSpan.FromMinutes(20), 1),
            new InstallCommand("apk", ["add", "ffmpeg"], "APK", TimeSpan.FromMinutes(20), 1)
        })
        {
            if (!elevated) yield return command;
            else yield return command with { FileName = "sudo", Arguments = [command.FileName, .. command.Arguments], DisplayName = $"sudo {command.DisplayName}" };
        }
    }

    /// <summary>
    /// Performs command exists for <see cref="FfmpegProvisioner"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg provisioner workflow.
    /// </summary>
    /// <param name="command">Command value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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

    /// <summary>
    /// Determines whether runnable for <see cref="FfmpegProvisioner"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg provisioner workflow.
    /// </summary>
    /// <param name="executable">Executable value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private static async Task<bool> IsRunnableAsync(string executable, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add("-version");
            if (!process.Start()) return false;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Performs run for <see cref="FfmpegProvisioner"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg provisioner workflow.
    /// </summary>
    /// <param name="command">Install command dependency used by the FFmpeg provisioner workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The process run result produced by the operation.</returns>
    private static async Task<ProcessRunResult> RunAsync(InstallCommand command, ILogger logger, CancellationToken cancellationToken)
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
            if (!process.Start()) return new ProcessRunResult(-1, false);

            var lastActivityTicks = DateTimeOffset.UtcNow.UtcDateTime.Ticks;
            void MarkActivity() => Interlocked.Exchange(ref lastActivityTicks, DateTimeOffset.UtcNow.UtcDateTime.Ticks);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(command.Timeout);

            var stdout = PumpOutputAsync(process.StandardOutput, line => logger.LogInformation("{Line}", line), MarkActivity, linked.Token);
            var stderr = PumpOutputAsync(process.StandardError, line => logger.LogWarning("{Line}", line), MarkActivity, linked.Token);
            var started = Stopwatch.StartNew();
            var waitForExit = process.WaitForExitAsync(CancellationToken.None);

            while (!waitForExit.IsCompleted)
            {
                var delay = Task.Delay(ProgressHeartbeat, linked.Token);
                var completed = await Task.WhenAny(waitForExit, delay).ConfigureAwait(false);
                if (completed == waitForExit) break;

                if (linked.IsCancellationRequested)
                    break;

                var lastActivity = new DateTimeOffset(Interlocked.Read(ref lastActivityTicks), TimeSpan.Zero);
                logger.LogInformation(
                    "{Manager} is still running ({Elapsed:mm\\:ss} elapsed; last output {Silence:mm\\:ss} ago).",
                    command.DisplayName,
                    started.Elapsed,
                    DateTimeOffset.UtcNow - lastActivity);
            }

            if (!waitForExit.IsCompleted)
            {
                try
                {
                    if (!process.HasExited) process.Kill(entireProcessTree: true);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Could not stop timed-out {Manager} process.", command.DisplayName);
                }

                try { await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false); } catch { }
                try { await Task.WhenAll(stdout, stderr).ConfigureAwait(false); } catch { }
                cancellationToken.ThrowIfCancellationRequested();
                return new ProcessRunResult(-1, true);
            }

            linked.Cancel();
            try { await Task.WhenAll(stdout, stderr).ConfigureAwait(false); } catch (OperationCanceledException) { }
            return new ProcessRunResult(process.ExitCode, false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ProcessRunResult(-1, true);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not run {Manager}.", command.DisplayName);
            return new ProcessRunResult(-1, false);
        }
    }

    /// <summary>
    /// Performs pump output for <see cref="FfmpegProvisioner"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg provisioner workflow.
    /// </summary>
    /// <param name="reader">Reader value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    /// <param name="writeLine">Write line value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    /// <param name="markActivity">Mark activity value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private static async Task PumpOutputAsync(
        StreamReader reader,
        Action<string> writeLine,
        Action markActivity,
        CancellationToken cancellationToken)
    {
        var buffer = new char[2048];
        var pending = new StringBuilder();
        try
        {
            while (true)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (read == 0) break;
                markActivity();

                for (var index = 0; index < read; index++)
                {
                    var character = buffer[index];
                    if (character is '\r' or '\n')
                    {
                        FlushPending(pending, writeLine);
                        continue;
                    }
                    pending.Append(character);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            FlushPending(pending, writeLine);
        }
    }

    /// <summary>
    /// Performs flush pending for <see cref="FfmpegProvisioner"/>, keeping the operation consistent with the state and invariants of the surrounding FFmpeg provisioner workflow.
    /// </summary>
    /// <param name="pending">Pending value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    /// <param name="writeLine">Write line value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    private static void FlushPending(StringBuilder pending, Action<string> writeLine)
    {
        if (pending.Length == 0) return;
        var line = pending.ToString().Trim();
        pending.Clear();
        if (!string.IsNullOrWhiteSpace(line)) writeLine(line);
    }

    /// <summary>
    /// Represents an install command helper type nested within <see cref="FfmpegProvisioner"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="FileName">File name value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    /// <param name="Arguments">Arguments value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    /// <param name="DisplayName">Display name value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    /// <param name="Timeout">Timeout value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    /// <param name="MaxAttempts">Max attempts value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    private sealed record InstallCommand(
        string FileName,
        string[] Arguments,
        string DisplayName,
        TimeSpan Timeout,
        int MaxAttempts);

    /// <summary>
    /// Represents the outcome of process run, carrying the data and status produced by the corresponding application operation.
    /// </summary>
    /// <param name="ExitCode">Exit code value supplied to the FFmpeg provisioner operation and used when producing its result.</param>
    /// <param name="TimedOut">Value indicating whether timed out should apply to this operation.</param>
    private sealed record ProcessRunResult(int ExitCode, bool TimedOut);
}

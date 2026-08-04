using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PublisherStudio.InstallerConsole.Installation;

internal sealed class PublisherStudioDeploymentService(ILogger logger)
{
    private const string ManifestFileName = "publisherstudio-release.json";
    private const string BootstrapRepairManifestFileName = "publisherstudio-bootstrap-repair.json";
    private const long MaximumExpandedBytes = 12L * 1024 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public void DeployApplication(string zipPath, PublisherStudioInstallLayout layout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipPath);
        Directory.CreateDirectory(layout.UpdateDirectory);
        CleanupAbandonedTransactions(layout.UpdateDirectory);
        var transactionRoot = Path.Combine(layout.UpdateDirectory, $"application-{Guid.NewGuid():N}");
        var extractedRoot = Path.Combine(transactionRoot, "extracted");
        Directory.CreateDirectory(extractedRoot);
        try
        {
            ExtractValidatedArchive(zipPath, extractedRoot, "Application", layout.ApplicationExecutableName, layout.RuntimeIdentifier);
            var payloadRoot = ResolvePayloadRoot(extractedRoot, layout.ApplicationExecutableName);
            StopOwnedApplication(layout);
            MergeManagedPayload(payloadRoot, layout.ApplicationDirectory, transactionRoot, "application");
            logger.LogInformation(
                "PublisherStudio application payload was merged into {ApplicationDirectory} without replacing the application directory.",
                layout.ApplicationDirectory);
        }
        finally
        {
            DeleteDirectoryBestEffort(transactionRoot);
        }
    }

    public bool DeploySetup(string zipPath, PublisherStudioInstallLayout layout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipPath);
        Directory.CreateDirectory(layout.UpdateDirectory);
        CleanupAbandonedTransactions(layout.UpdateDirectory);
        var transactionRoot = Path.Combine(layout.UpdateDirectory, $"setup-{Guid.NewGuid():N}");
        var extractedRoot = Path.Combine(transactionRoot, "extracted");
        Directory.CreateDirectory(extractedRoot);
        var keepTransaction = false;
        try
        {
            ExtractValidatedArchive(zipPath, extractedRoot, "Setup", layout.SetupExecutableName, layout.RuntimeIdentifier);
            var payloadRoot = ResolvePayloadRoot(extractedRoot, layout.SetupExecutableName);
            var runningDirectory = Path.GetFullPath(AppContext.BaseDirectory);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && IsPathWithin(runningDirectory, layout.SetupDirectory))
            {
                ScheduleWindowsSetupReplacement(payloadRoot, layout.SetupDirectory, transactionRoot);
                keepTransaction = true;
                logger.LogInformation(
                    "PublisherStudio setup replacement was staged for {SetupDirectory} and will complete after this setup process exits.",
                    layout.SetupDirectory);
                return true;
            }

            MergeManagedPayload(payloadRoot, layout.SetupDirectory, transactionRoot, "setup");
            logger.LogInformation("PublisherStudio setup and launcher payload was replaced at {SetupDirectory}.", layout.SetupDirectory);
            return false;
        }
        finally
        {
            if (!keepTransaction)
                DeleteDirectoryBestEffort(transactionRoot);
        }
    }

    public static void ValidateArchive(string zipPath, string expectedPayloadKind, string expectedExecutable, string expectedRuntimeIdentifier, ILogger logger)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entries = ValidateEntries(archive, zipPath);
        var executableMatches = entries
            .Where(pair => !IsDirectoryEntry(pair.Value) && string.Equals(Path.GetFileName(pair.Key), expectedExecutable, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (executableMatches.Length != 1)
            throw new InvalidDataException($"Archive '{zipPath}' must contain exactly one expected executable '{expectedExecutable}', but contained {executableMatches.Length}.");

        var manifestMatches = entries
            .Where(pair => !IsDirectoryEntry(pair.Value) && string.Equals(Path.GetFileName(pair.Key), ManifestFileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (manifestMatches.Length == 0)
        {
            logger.LogWarning("Archive {ArchivePath} has no {ManifestFile}; accepting it as a legacy PublisherStudio package.", zipPath, ManifestFileName);
            return;
        }
        if (manifestMatches.Length != 1)
            throw new InvalidDataException($"Archive '{zipPath}' contains more than one {ManifestFileName}.");

        var manifestEntry = manifestMatches[0];
        PublisherStudioReleaseManifest manifest;
        using (var stream = manifestEntry.Value.Open())
        {
            manifest = JsonSerializer.Deserialize<PublisherStudioReleaseManifest>(stream, JsonOptions)
                ?? throw new InvalidDataException($"Archive '{zipPath}' contains an empty release manifest.");
        }

        if (manifest.SchemaVersion is < 1 or > 2)
            throw new InvalidDataException($"Archive '{zipPath}' uses unsupported release-manifest schema {manifest.SchemaVersion}.");
        if (!string.Equals(manifest.Product, "PublisherStudio", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Archive '{zipPath}' belongs to product '{manifest.Product}', not PublisherStudio.");
        if (!string.Equals(manifest.PayloadKind, expectedPayloadKind, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Archive '{zipPath}' payload kind is '{manifest.PayloadKind}', expected '{expectedPayloadKind}'.");
        if (!string.Equals(Path.GetFileName(manifest.Executable), expectedExecutable, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Archive '{zipPath}' manifest executable is '{manifest.Executable}', expected '{expectedExecutable}'.");
        if (!string.IsNullOrWhiteSpace(manifest.RuntimeIdentifier)
            && !string.Equals(manifest.RuntimeIdentifier, expectedRuntimeIdentifier, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Archive '{zipPath}' targets '{manifest.RuntimeIdentifier}', but this installation expects '{expectedRuntimeIdentifier}'.");

        if (manifest.SchemaVersion == 1)
        {
            logger.LogWarning("Archive {ArchivePath} uses legacy manifest schema 1 without file hashes.", zipPath);
            return;
        }

        if (manifest.Files is null || manifest.Files.Count == 0)
            throw new InvalidDataException($"Archive '{zipPath}' uses manifest schema 2 but contains no file catalogue.");

        var manifestDirectory = Path.GetDirectoryName(manifestEntry.Key)?.Replace('\\', '/') ?? string.Empty;
        var catalogued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in manifest.Files)
        {
            var relative = NormalizeEntryName(file.Path);
            if (string.IsNullOrWhiteSpace(relative) || !catalogued.Add(relative))
                throw new InvalidDataException($"Archive '{zipPath}' contains an invalid or duplicate manifest path '{file.Path}'.");
            var fullName = string.IsNullOrWhiteSpace(manifestDirectory) ? relative : $"{manifestDirectory}/{relative}";
            if (!entries.TryGetValue(fullName, out var entry))
                throw new InvalidDataException($"Archive '{zipPath}' is missing manifest file '{relative}'.");
            if (entry.Length != file.Length)
                throw new InvalidDataException($"Archive '{zipPath}' file '{relative}' has length {entry.Length}, expected {file.Length}.");
            using var entryStream = entry.Open();
            var hash = Convert.ToHexString(SHA256.HashData(entryStream));
            if (!string.Equals(hash, file.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Archive '{zipPath}' file '{relative}' failed SHA-256 validation.");
        }

        var executableRelative = NormalizeEntryName(manifest.Executable);
        if (!catalogued.Contains(executableRelative))
            throw new InvalidDataException($"Archive '{zipPath}' manifest does not catalogue executable '{manifest.Executable}'.");

        var payloadPrefix = string.IsNullOrWhiteSpace(manifestDirectory) ? string.Empty : $"{manifestDirectory}/";
        var allowedBootstrapEntries = ValidateBootstrapRepair(entries, zipPath, expectedPayloadKind, manifest);
        foreach (var pair in entries)
        {
            if (IsDirectoryEntry(pair.Value) || string.Equals(pair.Key, manifestEntry.Key, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!pair.Key.StartsWith(payloadPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (allowedBootstrapEntries.Contains(pair.Key)) continue;
                throw new InvalidDataException($"Archive '{zipPath}' contains file '{pair.Key}' outside its manifest payload root.");
            }
            var relative = pair.Key[payloadPrefix.Length..];
            if (!catalogued.Contains(relative))
                throw new InvalidDataException($"Archive '{zipPath}' contains uncatalogued file '{relative}'.");
        }
    }


    private static HashSet<string> ValidateBootstrapRepair(
        IReadOnlyDictionary<string, ZipArchiveEntry> entries,
        string zipPath,
        string expectedPayloadKind,
        PublisherStudioReleaseManifest releaseManifest)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.Equals(expectedPayloadKind, "Application", StringComparison.OrdinalIgnoreCase)) return allowed;

        var manifestMatches = entries
            .Where(pair => !IsDirectoryEntry(pair.Value) && string.Equals(Path.GetFileName(pair.Key), BootstrapRepairManifestFileName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var windowsPayload = releaseManifest.RuntimeIdentifier.StartsWith("win-", StringComparison.OrdinalIgnoreCase);
        if (manifestMatches.Length == 0)
        {
            if (windowsPayload)
                throw new InvalidDataException($"Archive '{zipPath}' is missing the launcher repair payload required for existing Windows installations.");
            return allowed;
        }
        if (manifestMatches.Length != 1)
            throw new InvalidDataException($"Archive '{zipPath}' contains more than one {BootstrapRepairManifestFileName}.");

        var bootstrapEntry = manifestMatches[0];
        PublisherStudioReleaseManifest bootstrap;
        using (var stream = bootstrapEntry.Value.Open())
        {
            bootstrap = JsonSerializer.Deserialize<PublisherStudioReleaseManifest>(stream, JsonOptions)
                ?? throw new InvalidDataException($"Archive '{zipPath}' contains an empty launcher repair manifest.");
        }
        if (bootstrap.SchemaVersion != 2
            || !string.Equals(bootstrap.Product, "PublisherStudio", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(bootstrap.PayloadKind, "SetupRepair", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Archive '{zipPath}' contains an invalid launcher repair manifest.");
        if (!string.Equals(bootstrap.RuntimeIdentifier, releaseManifest.RuntimeIdentifier, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Archive '{zipPath}' launcher repair runtime '{bootstrap.RuntimeIdentifier}' does not match application runtime '{releaseManifest.RuntimeIdentifier}'.");
        if (!string.Equals(Path.GetFileName(bootstrap.Executable), "PublisherStudio.Setup.repair.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Archive '{zipPath}' launcher repair executable is invalid: '{bootstrap.Executable}'.");
        if (bootstrap.Files is null || bootstrap.Files.Count == 0)
            throw new InvalidDataException($"Archive '{zipPath}' launcher repair manifest contains no files.");

        var bootstrapDirectory = Path.GetDirectoryName(bootstrapEntry.Key)?.Replace('\\', '/') ?? string.Empty;
        var expectedBootstrapDirectory = $"setup{releaseManifest.RuntimeIdentifier.Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)}";
        if (!string.Equals(bootstrapDirectory, expectedBootstrapDirectory, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Archive '{zipPath}' launcher repair root is '{bootstrapDirectory}', expected '{expectedBootstrapDirectory}'.");

        var catalogued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in bootstrap.Files)
        {
            var relative = NormalizeEntryName(file.Path);
            if (string.IsNullOrWhiteSpace(relative) || !catalogued.Add(relative))
                throw new InvalidDataException($"Archive '{zipPath}' contains an invalid or duplicate launcher repair path '{file.Path}'.");
            var fullName = $"{bootstrapDirectory}/{relative}";
            if (!entries.TryGetValue(fullName, out var entry))
                throw new InvalidDataException($"Archive '{zipPath}' is missing launcher repair file '{relative}'.");
            if (entry.Length != file.Length)
                throw new InvalidDataException($"Archive '{zipPath}' launcher repair file '{relative}' has length {entry.Length}, expected {file.Length}.");
            using var entryStream = entry.Open();
            var hash = Convert.ToHexString(SHA256.HashData(entryStream));
            if (!string.Equals(hash, file.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Archive '{zipPath}' launcher repair file '{relative}' failed SHA-256 validation.");
            allowed.Add(fullName);
        }
        var executableRelative = NormalizeEntryName(bootstrap.Executable);
        if (!catalogued.Contains(executableRelative))
            throw new InvalidDataException($"Archive '{zipPath}' launcher repair manifest does not catalogue '{bootstrap.Executable}'.");
        allowed.Add(bootstrapEntry.Key);

        var prefix = $"{bootstrapDirectory}/";
        foreach (var pair in entries)
        {
            if (IsDirectoryEntry(pair.Value) || !pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            if (!allowed.Contains(pair.Key))
                throw new InvalidDataException($"Archive '{zipPath}' contains uncatalogued launcher repair file '{pair.Key[prefix.Length..]}'.");
        }
        return allowed;
    }

    private void ExtractValidatedArchive(string zipPath, string destination, string expectedPayloadKind, string expectedExecutable, string expectedRuntimeIdentifier)
    {
        ValidateArchive(zipPath, expectedPayloadKind, expectedExecutable, expectedRuntimeIdentifier, logger);
        using var archive = ZipFile.OpenRead(zipPath);
        var destinationRoot = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            var normalized = NormalizeEntryName(entry.FullName);
            if (string.IsNullOrWhiteSpace(normalized)) continue;
            if (!seen.Add(normalized))
                throw new InvalidDataException($"Archive '{zipPath}' contains duplicate normalized path '{normalized}'.");
            var target = Path.GetFullPath(Path.Combine(destination, normalized.Replace('/', Path.DirectorySeparatorChar)));
            if (!target.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Archive '{zipPath}' contains a path outside the extraction root: '{entry.FullName}'.");
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                Directory.CreateDirectory(target);
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var input = entry.Open();
            using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
            ApplyUnixMode(entry, target);
        }
    }

    private static Dictionary<string, ZipArchiveEntry> ValidateEntries(ZipArchive archive, string zipPath)
    {
        if (archive.Entries.Count == 0)
            throw new InvalidDataException($"Archive '{zipPath}' contains no entries.");
        long expandedBytes = 0;
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            var normalized = NormalizeEntryName(entry.FullName);
            if (string.IsNullOrWhiteSpace(normalized)) continue;
            if (Path.IsPathRooted(normalized) || normalized.Split('/').Any(part => part is ".." or "."))
                throw new InvalidDataException($"Archive '{zipPath}' contains unsafe path '{entry.FullName}'.");
            if (normalized.Contains(':'))
                throw new InvalidDataException($"Archive '{zipPath}' contains an invalid path '{entry.FullName}'.");
            if (!entries.TryAdd(normalized, entry))
                throw new InvalidDataException($"Archive '{zipPath}' contains duplicate normalized path '{normalized}'.");
            if (IsSymbolicLink(entry))
                throw new InvalidDataException($"Archive '{zipPath}' contains unsupported symbolic link '{entry.FullName}'.");
            expandedBytes = checked(expandedBytes + Math.Max(0, entry.Length));
            if (expandedBytes > MaximumExpandedBytes)
                throw new InvalidDataException($"Archive '{zipPath}' expands beyond the {MaximumExpandedBytes:N0}-byte safety limit.");
        }
        return entries;
    }

    private static string NormalizeEntryName(string value)
        => string.Join("/", value.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries));

    private static string ResolvePayloadRoot(string extractedRoot, string expectedExecutable)
    {
        if (File.Exists(Path.Combine(extractedRoot, expectedExecutable))) return extractedRoot;
        var candidates = Directory.GetDirectories(extractedRoot)
            .Where(directory => File.Exists(Path.Combine(directory, expectedExecutable)))
            .ToArray();
        if (candidates.Length == 1) return candidates[0];
        throw new InvalidDataException($"Extracted payload does not place exactly one '{expectedExecutable}' at its root or inside a recognized wrapper directory.");
    }

    private void MergeManagedPayload(string payloadRoot, string destination, string transactionRoot, string payloadLabel)
    {
        Directory.CreateDirectory(destination);
        var backupRoot = Path.Combine(transactionRoot, "backup");
        var incomingFiles = Directory.EnumerateFiles(payloadRoot, "*", SearchOption.AllDirectories)
            .Select(path => new DeploymentFile(path, NormalizeRelativePath(Path.GetRelativePath(payloadRoot, path))))
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (incomingFiles.Length == 0)
            throw new InvalidDataException($"The PublisherStudio {payloadLabel} payload contains no files.");

        var incomingPaths = incomingFiles.Select(file => file.RelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var previousManifest = TryReadInstalledManifest(destination, payloadLabel);
        var staleManagedFiles = new List<StaleDeploymentFile>();
        foreach (var file in previousManifest?.Files ?? [])
        {
            try
            {
                var relativePath = NormalizeRelativePath(file.Path);
                if (!incomingPaths.Contains(relativePath))
                    staleManagedFiles.Add(new StaleDeploymentFile(relativePath, file.Sha256));
            }
            catch (InvalidDataException exception)
            {
                logger.LogWarning(exception, "Ignoring unsafe path {ManifestPath} in the previously installed PublisherStudio {PayloadLabel} manifest.", file.Path, payloadLabel);
            }
        }

        var backedUp = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var created = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var removedStalePaths = new List<string>();
        try
        {
            foreach (var incoming in incomingFiles)
            {
                var target = ResolveDestinationPath(destination, incoming.RelativePath);
                BackupTargetIfPresent(target, incoming.RelativePath, backupRoot, backedUp, created);
                CopyFileAtomically(incoming.SourcePath, target);
            }

            foreach (var stale in staleManagedFiles)
            {
                var target = ResolveDestinationPath(destination, stale.RelativePath);
                if (!File.Exists(target)) continue;
                if (!FileMatchesManifestHash(target, stale.Sha256))
                {
                    logger.LogWarning(
                        "Preserving stale PublisherStudio {PayloadLabel} file {RelativePath} because it was modified after the previous release or lacks a trustworthy prior hash.",
                        payloadLabel,
                        stale.RelativePath);
                    continue;
                }
                BackupTargetIfPresent(target, stale.RelativePath, backupRoot, backedUp, created);
                File.Delete(target);
                removedStalePaths.Add(stale.RelativePath);
            }

            RemoveEmptyManagedDirectories(destination, removedStalePaths);
            logger.LogInformation(
                "PublisherStudio {PayloadLabel} merge wrote {WrittenCount} files, removed {StaleCount} unchanged manifest-owned stale files, and preserved unrelated or modified files.",
                payloadLabel,
                incomingFiles.Length,
                removedStalePaths.Count);
        }
        catch
        {
            RollBackManagedMerge(destination, backupRoot, backedUp, created);
            throw;
        }
    }

    private PublisherStudioReleaseManifest? TryReadInstalledManifest(string destination, string payloadLabel)
    {
        var manifestPath = Path.Combine(destination, ManifestFileName);
        if (!File.Exists(manifestPath)) return null;
        try
        {
            var manifest = JsonSerializer.Deserialize<PublisherStudioReleaseManifest>(File.ReadAllText(manifestPath), JsonOptions);
            if (manifest is null
                || manifest.SchemaVersion != 2
                || !string.Equals(manifest.Product, "PublisherStudio", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(manifest.PayloadKind, payloadLabel, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Ignoring incompatible installed PublisherStudio manifest {ManifestPath}; stale files will be preserved.", manifestPath);
                return null;
            }
            return manifest;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Ignoring unreadable installed PublisherStudio manifest {ManifestPath}; stale files will be preserved.", manifestPath);
            return null;
        }
    }

    private static bool FileMatchesManifestHash(string path, string expectedSha256)
    {
        if (string.IsNullOrWhiteSpace(expectedSha256) || expectedSha256.Length != 64) return false;
        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(SHA256.HashData(stream));
        return string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }

    private static void BackupTargetIfPresent(
        string target,
        string relativePath,
        string backupRoot,
        HashSet<string> backedUp,
        HashSet<string> created)
    {
        if (File.Exists(target))
        {
            if (!backedUp.Add(relativePath)) return;
            var backup = ResolveDestinationPath(backupRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
            File.Copy(target, backup, overwrite: true);
            return;
        }
        created.Add(relativePath);
    }

    private static void CopyFileAtomically(string source, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = $"{destination}.publisherstudio-{Guid.NewGuid():N}.tmp";
        try
        {
            File.Copy(source, temporary, overwrite: false);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try { File.SetUnixFileMode(temporary, File.GetUnixFileMode(source)); }
                catch (PlatformNotSupportedException) { }
            }
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static void RollBackManagedMerge(
        string destination,
        string backupRoot,
        IEnumerable<string> backedUp,
        IEnumerable<string> created)
    {
        foreach (var relativePath in created.OrderByDescending(path => path.Length))
        {
            var target = ResolveDestinationPath(destination, relativePath);
            if (File.Exists(target)) File.Delete(target);
        }
        foreach (var relativePath in backedUp)
        {
            var backup = ResolveDestinationPath(backupRoot, relativePath);
            if (!File.Exists(backup)) continue;
            var target = ResolveDestinationPath(destination, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(backup, target, overwrite: true);
        }
    }

    private static void RemoveEmptyManagedDirectories(string destination, IEnumerable<string> staleManagedPaths)
    {
        var root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var relativePath in staleManagedPaths.OrderByDescending(path => path.Length))
        {
            var directory = Path.GetDirectoryName(ResolveDestinationPath(destination, relativePath));
            while (!string.IsNullOrWhiteSpace(directory)
                   && !string.Equals(directory, root, StringComparison.OrdinalIgnoreCase)
                   && Directory.Exists(directory)
                   && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
                directory = Path.GetDirectoryName(directory);
            }
        }
    }

    private static string NormalizeRelativePath(string value)
    {
        var normalized = NormalizeEntryName(value);
        if (string.IsNullOrWhiteSpace(normalized)
            || Path.IsPathRooted(normalized)
            || normalized.Split('/').Any(part => part is "." or "..")
            || normalized.Contains(':'))
            throw new InvalidDataException($"Invalid PublisherStudio payload path '{value}'.");
        return normalized;
    }

    private static string ResolveDestinationPath(string root, string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath);
        var rootPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        if (!target.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Payload path '{relativePath}' escapes destination '{root}'.");
        return target;
    }

    private void StopOwnedApplication(PublisherStudioInstallLayout layout)
    {
        var endpointPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PublisherStudio", "runtime", "server.json");
        if (!File.Exists(endpointPath)) return;
        var removeEndpoint = false;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(endpointPath));
            if (!document.RootElement.TryGetProperty("ProcessId", out var processIdElement) || !processIdElement.TryGetInt32(out var processId) || processId <= 0)
            {
                removeEndpoint = true;
                return;
            }
            using var process = Process.GetProcessById(processId);
            process.Refresh();
            if (process.HasExited)
            {
                removeEndpoint = true;
                return;
            }
            string? executablePath = null;
            try { executablePath = process.MainModule?.FileName; }
            catch (Exception exception) { logger.LogDebug(exception, "Could not inspect the running PublisherStudio executable path."); }
            if (string.IsNullOrWhiteSpace(executablePath) || !IsPathWithin(executablePath, layout.RootDirectory))
                throw new InvalidOperationException($"Refusing to stop process {processId}; its executable is not owned by installation root '{layout.RootDirectory}'.");
            logger.LogInformation("Stopping running PublisherStudio process {ProcessId} before application update.", processId);
            try { process.CloseMainWindow(); } catch (InvalidOperationException) { }
            if (!process.WaitForExit(8000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(10000);
            }
            removeEndpoint = true;
        }
        catch (ArgumentException)
        {
            removeEndpoint = true;
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("has exited", StringComparison.OrdinalIgnoreCase))
        {
            removeEndpoint = true;
        }
        finally
        {
            if (removeEndpoint)
            {
                try { File.Delete(endpointPath); }
                catch (Exception exception) { logger.LogDebug(exception, "Could not remove the stale PublisherStudio runtime endpoint file."); }
            }
        }
    }

    private void ScheduleWindowsSetupReplacement(string payloadRoot, string destination, string transactionRoot)
    {
        var scriptPath = Path.Combine(transactionRoot, "Apply-PublisherStudioSetupUpdate.ps1");
        var logPath = Path.Combine(transactionRoot, "setup-update.log");
        var backupPath = Path.Combine(transactionRoot, "setup-backup");
        var script = """
param(
    [Parameter(Mandatory=$true)][int]$ParentProcessId,
    [Parameter(Mandatory=$true)][string]$SourceDirectory,
    [Parameter(Mandatory=$true)][string]$DestinationDirectory,
    [Parameter(Mandatory=$true)][string]$BackupDirectory,
    [Parameter(Mandatory=$true)][string]$LogPath
)
$ErrorActionPreference = 'Stop'
$created = New-Object 'System.Collections.Generic.List[string]'
$backedUp = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
try {
    $deadline = [DateTimeOffset]::UtcNow.AddMinutes(5)
    while (Get-Process -Id $ParentProcessId -ErrorAction SilentlyContinue) {
        if ([DateTimeOffset]::UtcNow -ge $deadline) { throw "Timed out waiting for PublisherStudio setup process $ParentProcessId to exit." }
        Start-Sleep -Milliseconds 250
    }

    $sourceRoot = [IO.Path]::GetFullPath($SourceDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $destinationRoot = [IO.Path]::GetFullPath($DestinationDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null

    $oldManaged = @()
    $oldManifestPath = Join-Path $destinationRoot 'publisherstudio-release.json'
    if (Test-Path -LiteralPath $oldManifestPath -PathType Leaf) {
        try {
            $oldManifest = Get-Content -LiteralPath $oldManifestPath -Raw | ConvertFrom-Json
            if ([int]$oldManifest.SchemaVersion -eq 2) { $oldManaged = @($oldManifest.Files) }
        }
        catch { $oldManaged = @() }
    }

    $incoming = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $sourceFiles = @(Get-ChildItem -LiteralPath $sourceRoot -File -Recurse | Sort-Object FullName)
    foreach ($sourceFile in $sourceFiles) {
        $relative = $sourceFile.FullName.Substring($sourceRoot.Length).TrimStart([char[]]'\/').Replace('\', '/')
        if ($relative.Split('/') -contains '..' -or [IO.Path]::IsPathRooted($relative)) { throw "Unsafe staged setup path '$relative'." }
        [void]$incoming.Add($relative)
        $target = Join-Path $destinationRoot $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)
        $backup = Join-Path $BackupDirectory $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $target -PathType Leaf) {
            if ($backedUp.Add($relative)) {
                New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
                Copy-Item -LiteralPath $target -Destination $backup -Force
            }
        }
        else { $created.Add($relative) }
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        $temporary = "$target.publisherstudio-$([Guid]::NewGuid().ToString('N')).tmp"
        Copy-Item -LiteralPath $sourceFile.FullName -Destination $temporary -Force
        Move-Item -LiteralPath $temporary -Destination $target -Force
    }

    foreach ($managedFile in $oldManaged) {
        $normalized = ([string]$managedFile.Path).Replace('\', '/').Trim('/')
        if ([string]::IsNullOrWhiteSpace($normalized) -or $incoming.Contains($normalized)) { continue }
        if ($normalized.Split('/') -contains '..' -or [IO.Path]::IsPathRooted($normalized)) { continue }
        $target = Join-Path $destinationRoot $normalized.Replace('/', [IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { continue }
        $expectedHash = [string]$managedFile.Sha256
        if ([string]::IsNullOrWhiteSpace($expectedHash) -or $expectedHash.Length -ne 64) { continue }
        $actualHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        if (-not [string]::Equals($actualHash, $expectedHash, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $backup = Join-Path $BackupDirectory $normalized.Replace('/', [IO.Path]::DirectorySeparatorChar)
        if ($backedUp.Add($normalized)) {
            New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
            Copy-Item -LiteralPath $target -Destination $backup -Force
        }
        Remove-Item -LiteralPath $target -Force
    }

    "PublisherStudio setup file merge completed at $([DateTimeOffset]::UtcNow.ToString('O'))." | Set-Content -LiteralPath $LogPath -Encoding UTF8
}
catch {
    foreach ($relative in ($created | Sort-Object Length -Descending)) {
        $target = Join-Path $DestinationDirectory $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)
        Remove-Item -LiteralPath $target -Force -ErrorAction SilentlyContinue
    }
    foreach ($relative in $backedUp) {
        $backup = Join-Path $BackupDirectory $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)
        $target = Join-Path $DestinationDirectory $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $backup -PathType Leaf) {
            New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
            Copy-Item -LiteralPath $backup -Destination $target -Force
        }
    }
    $_ | Out-String | Set-Content -LiteralPath $LogPath -Encoding UTF8
    exit 1
}
""";
        File.WriteAllText(scriptPath, script, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var powershellPath = Path.Combine(systemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        if (!File.Exists(powershellPath)) powershellPath = "powershell.exe";
        var startInfo = new ProcessStartInfo
        {
            FileName = powershellPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-ParentProcessId");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("-SourceDirectory");
        startInfo.ArgumentList.Add(payloadRoot);
        startInfo.ArgumentList.Add("-DestinationDirectory");
        startInfo.ArgumentList.Add(destination);
        startInfo.ArgumentList.Add("-BackupDirectory");
        startInfo.ArgumentList.Add(backupPath);
        startInfo.ArgumentList.Add("-LogPath");
        startInfo.ArgumentList.Add(logPath);
        _ = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the PublisherStudio setup replacement helper.");
    }

    private static bool IsPathWithin(string candidate, string root)
    {
        var fullCandidate = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(fullCandidate, fullRoot, StringComparison.OrdinalIgnoreCase)
            || fullCandidate.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDirectoryEntry(ZipArchiveEntry entry)
        => string.IsNullOrWhiteSpace(entry.Name) || entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\');

    private static bool IsSymbolicLink(ZipArchiveEntry entry)
    {
        const int UnixFileTypeMask = 0xF000;
        const int UnixSymbolicLink = 0xA000;
        var mode = (entry.ExternalAttributes >> 16) & 0xFFFF;
        return (mode & UnixFileTypeMask) == UnixSymbolicLink;
    }

    private static void ApplyUnixMode(ZipArchiveEntry entry, string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        var rawMode = (entry.ExternalAttributes >> 16) & 0x1FF;
        if (rawMode == 0) return;
        try { File.SetUnixFileMode(path, (UnixFileMode)rawMode); }
        catch (PlatformNotSupportedException) { }
    }

    private void CleanupAbandonedTransactions(string updateDirectory)
    {
        if (!Directory.Exists(updateDirectory)) return;
        var cutoff = DateTime.UtcNow.AddDays(-2);
        foreach (var directory in Directory.EnumerateDirectories(updateDirectory))
        {
            try
            {
                if (Directory.GetLastWriteTimeUtc(directory) < cutoff)
                    Directory.Delete(directory, recursive: true);
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Could not remove abandoned installer transaction directory {TransactionDirectory}.", directory);
            }
        }
    }

    private void DeleteDirectoryBestEffort(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch (Exception exception) { logger.LogDebug(exception, "Could not remove installer transaction directory {TransactionDirectory}.", path); }
    }

    private sealed record DeploymentFile(string SourcePath, string RelativePath);
    private sealed record StaleDeploymentFile(string RelativePath, string Sha256);
}

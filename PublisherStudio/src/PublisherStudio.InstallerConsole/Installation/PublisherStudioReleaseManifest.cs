using System;
using System.Collections.Generic;

namespace PublisherStudio.InstallerConsole.Installation;

internal sealed class PublisherStudioReleaseManifest
{
    public int SchemaVersion { get; set; }
    public string Product { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string RuntimeIdentifier { get; set; } = string.Empty;
    public string PayloadKind { get; set; } = string.Empty;
    public string Executable { get; set; } = string.Empty;
    public string WireProtocolVersion { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public List<PublisherStudioReleaseFile> Files { get; set; } = [];
}

internal sealed class PublisherStudioReleaseFile
{
    public string Path { get; set; } = string.Empty;
    public long Length { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}

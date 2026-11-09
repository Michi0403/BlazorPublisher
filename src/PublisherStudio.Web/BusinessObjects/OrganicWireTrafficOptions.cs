namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Configures the 1-Wire traffic journal (appsettings section <c>OrganicPlugins:Traffic</c>; changes apply while PublisherStudio runs).
/// Every packet PublisherStudio sends or receives over TCP, HTTP/JSON or the UDP discovery beacon is journaled: an Information
/// log line with its metadata, the decrypted payload at Debug and the raw wire frame at Trace, plus the live in-app view.
/// </summary>
public sealed class OrganicWireTrafficOptions
{
    /// <summary>The configuration section bound live to these options.</summary>
    public const string SectionName = "OrganicPlugins:Traffic";

    /// <summary>Gets or sets whether 1-Wire traffic is journaled at all.</summary>
    /// <value><see langword="true"/> to log and show every packet; the default.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>Gets or sets how much of each packet's content is captured: Full, Redacted or None.</summary>
    /// <value>One of the <see cref="OrganicWireTrafficPayloadModes"/> values; Full by default.</value>
    public string PayloadMode { get; set; } = OrganicWireTrafficPayloadModes.Full;
    /// <summary>Gets or sets whether the frame exactly as it crossed the wire (encrypted when protected) is captured too.</summary>
    /// <value><see langword="true"/> to capture the raw frame (logged at Trace); the default.</value>
    public bool IncludeRawFrame { get; set; } = true;
    /// <summary>Gets or sets whether the periodic UDP discovery beacons are journaled.</summary>
    /// <value><see langword="true"/> to journal discovery beacons; the default.</value>
    public bool IncludeDiscovery { get; set; } = true;
    /// <summary>Gets or sets how many recent packets the in-app traffic view keeps.</summary>
    /// <value>The journal capacity; values below 10 are raised to 10.</value>
    public int JournalCapacity { get; set; } = 1000;
    /// <summary>Gets or sets the maximum characters of payload and raw frame kept per packet in the in-app view.</summary>
    /// <value>The per-packet view bound; 0 keeps everything.</value>
    public int MaximumStoredCharacters { get; set; } = 262144;
    /// <summary>Gets or sets the maximum characters of payload and raw frame written per log line.</summary>
    /// <value>The per-line log bound; 0 (the default) logs everything.</value>
    public int MaximumLoggedCharacters { get; set; }
    /// <summary>Gets or sets the JSON property names (case-insensitive, matched as substrings) masked in Redacted mode.</summary>
    /// <value>The property names; when empty, a built-in list of secret-like names is used.</value>
    public string[] RedactedPropertyNames { get; set; } = [];
}

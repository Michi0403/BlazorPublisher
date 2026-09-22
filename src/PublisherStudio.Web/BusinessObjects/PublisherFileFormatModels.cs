namespace PublisherStudio.BusinessObjects;

/// <summary>Describes a maintained PublisherStudio file-format family and the runtime conditions under which linked clients may use it.</summary>
public sealed class PublisherFileFormatFamily
{
    /// <summary>Identifies this format family with the stable key consumed by LocalGPT and 1-Wire capability routing.</summary>
    /// <value>The stable, machine-readable family identifier.</value>
    public string Key { get; set; } = string.Empty;

    /// <summary>Provides the user-facing name shown when PublisherStudio advertises this family to linked workflows.</summary>
    /// <value>The localized-or-display-ready family name.</value>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Lists the normalized lowercase extensions PublisherStudio associates with this family for capability matching.</summary>
    /// <value>A collection of extensions including the leading dot.</value>
    public List<string> Extensions { get; set; } = [];

    /// <summary>Indicates whether PublisherStudio can ingest this family directly without first requiring the optional media conversion runtime.</summary>
    /// <value><see langword="true"/> when direct ingestion is available; otherwise <see langword="false"/>.</value>
    public bool DirectImportAvailable { get; set; }

    /// <summary>Indicates whether processing this family depends on PublisherStudio's optional media conversion runtime.</summary>
    /// <value><see langword="true"/> when the media runtime is required for processing; otherwise <see langword="false"/>.</value>
    public bool RequiresMediaRuntime { get; set; }

    /// <summary>Reports whether the runtime required by this family is available on the PublisherStudio node producing the capability snapshot.</summary>
    /// <value><see langword="true"/> when the required runtime is ready or unnecessary; otherwise <see langword="false"/>.</value>
    public bool RuntimeAvailable { get; set; } = true;
}

/// <summary>Captures PublisherStudio's current file-format capability contract so LocalGPT and other linked clients can route work without guessing.</summary>
public sealed class PublisherFileFormatCapabilitySnapshot
{
    /// <summary>Collects the format families currently advertised by this PublisherStudio runtime for linked processing workflows.</summary>
    /// <value>The current set of maintained format-family capability records.</value>
    public List<PublisherFileFormatFamily> Families { get; set; } = [];

    /// <summary>Reports whether PublisherStudio's optional media conversion runtime is available for families that depend on it.</summary>
    /// <value><see langword="true"/> when media conversion is currently available; otherwise <see langword="false"/>.</value>
    public bool MediaConversionAvailable { get; set; }

    /// <summary>Records when this capability snapshot was produced so linked clients can reason about the freshness of runtime availability.</summary>
    /// <value>The UTC timestamp at which the snapshot was created.</value>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

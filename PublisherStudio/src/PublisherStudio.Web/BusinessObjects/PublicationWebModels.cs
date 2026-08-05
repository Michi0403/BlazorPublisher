using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Describes how PublisherStudio obtains a web resource. The same contract is intentionally
/// independent from charts so it can later be reused by web-content frames, automation,
/// network playback, and streaming providers.
/// </summary>
public sealed class PublicationWebBinding
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets transport.
    /// </summary>
    public PublicationWebTransportKind Transport { get; set; } = PublicationWebTransportKind.MonolithApi;
    /// <summary>
    /// Gets or sets method.
    /// </summary>
    public PublicationWebHttpMethod Method { get; set; } = PublicationWebHttpMethod.Get;
    /// <summary>
    /// Gets or sets URL.
    /// </summary>
    public string Url { get; set; } = "/api/publisher/system/status";
    /// <summary>
    /// Gets or sets headers.
    /// </summary>
    public List<PublicationWebHeader> Headers { get; set; } = [];
    /// <summary>
    /// Gets or sets request body.
    /// </summary>
    public string RequestBody { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets response format.
    /// </summary>
    public PublicationWebResponseFormat ResponseFormat { get; set; } = PublicationWebResponseFormat.Auto;
    /// <summary>
    /// Gets or sets JSON path.
    /// </summary>
    public string JsonPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets delimiter.
    /// </summary>
    public string Delimiter { get; set; } = ",";
    /// <summary>
    /// Gets or sets first row contains headers.
    /// </summary>
    public bool FirstRowContainsHeaders { get; set; } = true;
    /// <summary>
    /// Gets or sets refresh interval seconds.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 30;
    /// <summary>
    /// Gets or sets timeout seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets refresh on open.
    /// </summary>
    public bool RefreshOnOpen { get; set; } = true;
    /// <summary>
    /// Gets or sets allow exported HTML fetch.
    /// </summary>
    public bool AllowExportedHtmlFetch { get; set; } = false;
    /// <summary>
    /// Gets or sets use snapshot on failure.
    /// </summary>
    public bool UseSnapshotOnFailure { get; set; } = true;
    /// <summary>
    /// Gets or sets webhook token.
    /// </summary>
    public string WebhookToken { get; set; } = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
    // Tokenized CORS endpoint used only when a user explicitly allows a standalone
    // HTML export to reconnect to the local PublisherStudio monolith.
    /// <summary>
    /// Gets or sets export access token.
    /// </summary>
    public string ExportAccessToken { get; set; } = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
    /// <summary>
    /// Gets or sets last success UTC.
    /// </summary>
    public DateTimeOffset? LastSuccessUtc { get; set; }
    /// <summary>
    /// Gets or sets last content type.
    /// </summary>
    public string LastContentType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets last error.
    /// </summary>
    public string LastError { get; set; } = string.Empty;

    /// <summary>
    /// Gets is push.
    /// </summary>
    [JsonIgnore]
    public bool IsPush => Transport == PublicationWebTransportKind.Webhook;
}

/// <summary>
/// Represents a publication web header.
/// </summary>
public sealed class PublicationWebHeader
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets value.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Lists supported publication web transport kind values.
/// </summary>
public enum PublicationWebTransportKind
{
    MonolithApi,
    RestApi,
    Webhook,
    Stream
}

/// <summary>
/// Lists supported publication web HTTP method values.
/// </summary>
public enum PublicationWebHttpMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete
}

/// <summary>
/// Lists supported publication web response format values.
/// </summary>
public enum PublicationWebResponseFormat
{
    Auto,
    Json,
    DelimitedText,
    Xml,
    Text
}

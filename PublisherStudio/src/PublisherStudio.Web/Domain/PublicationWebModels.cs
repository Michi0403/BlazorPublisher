using System.Text.Json.Serialization;

namespace PublisherStudio.Domain;

/// <summary>
/// Describes how PublisherStudio obtains a web resource. The same contract is intentionally
/// independent from charts so it can later be reused by web-content frames, automation,
/// network playback, and streaming providers.
/// </summary>
public sealed class PublicationWebBinding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PublicationWebTransportKind Transport { get; set; } = PublicationWebTransportKind.MonolithApi;
    public PublicationWebHttpMethod Method { get; set; } = PublicationWebHttpMethod.Get;
    public string Url { get; set; } = "/api/publisher/system/status";
    public List<PublicationWebHeader> Headers { get; set; } = [];
    public string RequestBody { get; set; } = string.Empty;
    public PublicationWebResponseFormat ResponseFormat { get; set; } = PublicationWebResponseFormat.Auto;
    public string JsonPath { get; set; } = string.Empty;
    public string Delimiter { get; set; } = ",";
    public bool FirstRowContainsHeaders { get; set; } = true;
    public int RefreshIntervalSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 30;
    public bool Enabled { get; set; } = true;
    public bool RefreshOnOpen { get; set; } = true;
    public bool AllowExportedHtmlFetch { get; set; } = false;
    public bool UseSnapshotOnFailure { get; set; } = true;
    public string WebhookToken { get; set; } = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
    // Tokenized CORS endpoint used only when a user explicitly allows a standalone
    // HTML export to reconnect to the local PublisherStudio monolith.
    public string ExportAccessToken { get; set; } = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
    public DateTimeOffset? LastSuccessUtc { get; set; }
    public string LastContentType { get; set; } = string.Empty;
    public string LastError { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsPush => Transport == PublicationWebTransportKind.Webhook;
}

public sealed class PublicationWebHeader
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public enum PublicationWebTransportKind
{
    MonolithApi,
    RestApi,
    Webhook,
    Stream
}

public enum PublicationWebHttpMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete
}

public enum PublicationWebResponseFormat
{
    Auto,
    Json,
    DelimitedText,
    Xml,
    Text
}

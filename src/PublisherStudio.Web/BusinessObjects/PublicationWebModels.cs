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
    /// Gets or sets the stable identifier used to identify or correlate this publication web binding instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationWebBinding"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the transport value that forms part of the publication web binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The transport value exposed by <see cref="PublicationWebBinding"/>.</value>
    public PublicationWebTransportKind Transport { get; set; } = PublicationWebTransportKind.MonolithApi;
    /// <summary>
    /// Gets or sets the method value that forms part of the publication web binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method value exposed by <see cref="PublicationWebBinding"/>.</value>
    public PublicationWebHttpMethod Method { get; set; } = PublicationWebHttpMethod.Get;
    /// <summary>
    /// Gets or sets the URL that identifies the network or application endpoint associated with this publication web binding state.
    /// </summary>
    /// <value>The URL value exposed by <see cref="PublicationWebBinding"/>.</value>
    public string Url { get; set; } = "/api/publisher/system/status";
    /// <summary>
    /// Gets or sets the headers collection maintained or exposed by this publication web binding instance for downstream processing.
    /// </summary>
    /// <value>The headers value exposed by <see cref="PublicationWebBinding"/>.</value>
    public List<PublicationWebHeader> Headers { get; set; } = [];
    /// <summary>
    /// Gets or sets the request body value that forms part of the publication web binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The request body value exposed by <see cref="PublicationWebBinding"/>.</value>
    public string RequestBody { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the response format value that forms part of the publication web binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The response format value exposed by <see cref="PublicationWebBinding"/>.</value>
    public PublicationWebResponseFormat ResponseFormat { get; set; } = PublicationWebResponseFormat.Auto;
    /// <summary>
    /// Gets or sets the JSON path used by this publication web binding instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The JSON path value exposed by <see cref="PublicationWebBinding"/>.</value>
    public string JsonPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the delimiter value that forms part of the publication web binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The delimiter value exposed by <see cref="PublicationWebBinding"/>.</value>
    public string Delimiter { get; set; } = ",";
    /// <summary>
    /// Gets or sets a value indicating whether first row contains headers applies to the publication web binding state.
    /// </summary>
    /// <value>The first row contains headers value exposed by <see cref="PublicationWebBinding"/>.</value>
    public bool FirstRowContainsHeaders { get; set; } = true;
    /// <summary>
    /// Gets or sets the refresh interval seconds value that forms part of the publication web binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The refresh interval seconds value exposed by <see cref="PublicationWebBinding"/>.</value>
    public int RefreshIntervalSeconds { get; set; } = 30;
    /// <summary>
    /// Gets or sets the timeout seconds value that forms part of the publication web binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The timeout seconds value exposed by <see cref="PublicationWebBinding"/>.</value>
    public int TimeoutSeconds { get; set; } = 30;
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the publication web binding state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="PublicationWebBinding"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether refresh on open applies to the publication web binding state.
    /// </summary>
    /// <value>The refresh on open value exposed by <see cref="PublicationWebBinding"/>.</value>
    public bool RefreshOnOpen { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether exported HTML fetch applies to the publication web binding state.
    /// </summary>
    /// <value>The allow exported HTML fetch value exposed by <see cref="PublicationWebBinding"/>.</value>
    public bool AllowExportedHtmlFetch { get; set; } = false;
    /// <summary>
    /// Gets or sets a value indicating whether snapshot on failure applies to the publication web binding state.
    /// </summary>
    /// <value>The use snapshot on failure value exposed by <see cref="PublicationWebBinding"/>.</value>
    public bool UseSnapshotOnFailure { get; set; } = true;
    /// <summary>
    /// Gets or sets the webhook token value that forms part of the publication web binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The webhook token value exposed by <see cref="PublicationWebBinding"/>.</value>
    public string WebhookToken { get; set; } = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
    // Tokenized CORS endpoint used only when a user explicitly allows a standalone
    // HTML export to reconnect to the local PublisherStudio monolith.
    /// <summary>
    /// Gets or sets the export access token value that forms part of the publication web binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The export access token value exposed by <see cref="PublicationWebBinding"/>.</value>
    public string ExportAccessToken { get; set; } = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
    /// <summary>
    /// Gets or sets the last success UTC associated with this publication web binding state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last success UTC value exposed by <see cref="PublicationWebBinding"/>.</value>
    public DateTimeOffset? LastSuccessUtc { get; set; }
    /// <summary>
    /// Gets or sets the last content type value that forms part of the publication web binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last content type value exposed by <see cref="PublicationWebBinding"/>.</value>
    public string LastContentType { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last error value that forms part of the publication web binding state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last error value exposed by <see cref="PublicationWebBinding"/>.</value>
    public string LastError { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether push applies to the publication web binding state.
    /// </summary>
    /// <value>The is push value exposed by <see cref="PublicationWebBinding"/>.</value>
    [JsonIgnore]
    public bool IsPush => Transport == PublicationWebTransportKind.Webhook;
}

/// <summary>
/// Represents a publication web header application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationWebHeader
{
    /// <summary>
    /// Gets or sets the name value that forms part of the publication web header state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationWebHeader"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the value value that forms part of the publication web header state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value value exposed by <see cref="PublicationWebHeader"/>.</value>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Defines the supported publication web transport kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationWebTransportKind
{
    /// <summary>
    /// Selects the monolith API option for <see cref="PublicationWebTransportKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MonolithApi,
    /// <summary>
    /// Selects the rest API option for <see cref="PublicationWebTransportKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    RestApi,
    /// <summary>
    /// Selects the webhook option for <see cref="PublicationWebTransportKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Webhook,
    /// <summary>
    /// Selects the stream option for <see cref="PublicationWebTransportKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Stream
}

/// <summary>
/// Defines the supported publication web HTTP method values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationWebHttpMethod
{
    /// <summary>
    /// Selects the get option for <see cref="PublicationWebHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Get,
    /// <summary>
    /// Selects the post option for <see cref="PublicationWebHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Post,
    /// <summary>
    /// Selects the put option for <see cref="PublicationWebHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Put,
    /// <summary>
    /// Selects the patch option for <see cref="PublicationWebHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Patch,
    /// <summary>
    /// Selects the delete option for <see cref="PublicationWebHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Delete
}

/// <summary>
/// Defines the supported publication web response format values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationWebResponseFormat
{
    /// <summary>
    /// Selects the auto option for <see cref="PublicationWebResponseFormat"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Auto,
    /// <summary>
    /// Selects the JSON option for <see cref="PublicationWebResponseFormat"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Json,
    /// <summary>
    /// Selects the delimited text option for <see cref="PublicationWebResponseFormat"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DelimitedText,
    /// <summary>
    /// Selects the XML option for <see cref="PublicationWebResponseFormat"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Xml,
    /// <summary>
    /// Selects the text option for <see cref="PublicationWebResponseFormat"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Text
}

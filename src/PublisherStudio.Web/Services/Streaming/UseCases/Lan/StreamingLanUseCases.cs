using System.Net;

namespace PublisherStudio.Services.Streaming.UseCases.Lan;

/// <summary>
/// Resolves LAN status, safe HLS assets and the local watch page without coupling controllers to session internals.
/// </summary>
/// <param name="sessions">Media session registry dependency used by the streaming LAN use cases workflow to provide the corresponding application capability.</param>
public sealed class StreamingLanUseCases(MediaSessionRegistry sessions)
{
    /// <summary>
    /// Stores the media session registry dependency used by <see cref="StreamingLanUseCases"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly MediaSessionRegistry _sessions = sessions;

    /// <summary>
    /// Retrieves status for <see cref="StreamingLanUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming LAN use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>The streaming LAN status produced by the operation.</returns>
    public StreamingLanStatus? GetStatus(Guid sessionId)
    {
    try
    {
            if (!_sessions.TryGet(sessionId, out var session)) return null;
            return new StreamingLanStatus
            {
                SessionId = sessionId,
                Enabled = session.LanEnabled,
                Status = session.LanServer?.Status ?? "disabled",
                Error = session.LanServer?.LastError ?? string.Empty,
                BrowserUrl = session.LanServer?.BrowserUrl,
                HlsUrl = session.LanServer?.HlsUrl,
                RtspUrl = string.IsNullOrWhiteSpace(session.RtspUrl) ? session.LanServer?.RtspUrl : session.RtspUrl,
                AccessToken = session.LanServer?.AccessToken
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingLanUseCases.GetStatus failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Resolves asset for <see cref="StreamingLanUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming LAN use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="asset">Asset value supplied to the streaming LAN use cases operation and used when producing its result.</param>
    /// <returns>The streaming asset produced by the operation.</returns>
    public StreamingAsset? ResolveAsset(Guid sessionId, string? asset)
    {
    try
    {
            if (!_sessions.TryGet(sessionId, out var session) || string.IsNullOrWhiteSpace(session.HlsDirectory)) return null;
            var root = Path.GetFullPath(session.HlsDirectory);
            var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
            var relative = string.IsNullOrWhiteSpace(asset) ? "index.m3u8" : asset.Replace('/', Path.DirectorySeparatorChar);
            var candidate = Path.GetFullPath(Path.Combine(root, relative));
            if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidate)) return null;
            var contentType = Path.GetExtension(candidate).ToLowerInvariant() switch
            {
                ".m3u8" => "application/vnd.apple.mpegurl",
                ".ts" => "video/mp2t",
                ".m4s" => "video/iso.segment",
                _ => "application/octet-stream"
            };
            return new StreamingAsset(candidate, contentType);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingLanUseCases.ResolveAsset failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Builds watch page for <see cref="StreamingLanUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding streaming LAN use cases workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string? BuildWatchPage(Guid sessionId)
    {
    try
    {
            if (!_sessions.TryGet(sessionId, out var session) || !session.LanEnabled) return null;
            return $$"""
            <!doctype html><html><head><meta charset="utf-8"><title>PublisherStudio stream</title>
            <style>html,body{margin:0;background:#050b16;color:#fff;font:16px system-ui;height:100%}main{display:grid;place-items:center;height:100%}section{text-align:center;max-width:44rem}code{color:#93c5fd}</style></head>
            <body><main><section><h1>{{WebUtility.HtmlEncode(session.Name)}}</h1><p>The LAN output is prepared. The renderer/encoder must announce its WebRTC or HLS ingest before playback starts.</p><p>Session <code>{{sessionId:D}}</code></p></section></main></body></html>
            """;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method StreamingLanUseCases.BuildWatchPage failed: {__serviceMethodException}");
        throw;
    }
}
}

/// <summary>
/// Represents a streaming LAN status application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class StreamingLanStatus
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this streaming LAN status instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="StreamingLanStatus"/>.</value>
    public Guid SessionId { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the streaming LAN status state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="StreamingLanStatus"/>.</value>
    public bool Enabled { get; init; }
    /// <summary>
    /// Gets or sets the status value that forms part of the streaming LAN status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The status value exposed by <see cref="StreamingLanStatus"/>.</value>
    public string Status { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the error value that forms part of the streaming LAN status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The error value exposed by <see cref="StreamingLanStatus"/>.</value>
    public string Error { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the browser URL that identifies the network or application endpoint associated with this streaming LAN status state.
    /// </summary>
    /// <value>The browser URL value exposed by <see cref="StreamingLanStatus"/>.</value>
    public string? BrowserUrl { get; init; }
    /// <summary>
    /// Gets or sets the hls URL that identifies the network or application endpoint associated with this streaming LAN status state.
    /// </summary>
    /// <value>The hls URL value exposed by <see cref="StreamingLanStatus"/>.</value>
    public string? HlsUrl { get; init; }
    /// <summary>
    /// Gets or sets the rtsp URL that identifies the network or application endpoint associated with this streaming LAN status state.
    /// </summary>
    /// <value>The rtsp URL value exposed by <see cref="StreamingLanStatus"/>.</value>
    public string? RtspUrl { get; init; }
    /// <summary>
    /// Gets or sets the access token value that forms part of the streaming LAN status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The access token value exposed by <see cref="StreamingLanStatus"/>.</value>
    public string? AccessToken { get; init; }
}

/// <summary>
/// Represents a streaming asset application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Path">Path value supplied to the streaming asset operation and used when producing its result.</param>
/// <param name="ContentType">Content type value supplied to the streaming asset operation and used when producing its result.</param>
public sealed record StreamingAsset(string Path, string ContentType);

using System.Net;

namespace PublisherStudio.Services.Streaming.UseCases.Lan;

/// <summary>
/// Resolves LAN status, safe HLS assets and the local watch page without coupling MVC to backend state.
/// </summary>
public sealed class StreamingLanUseCases(MediaSessionRegistry sessions)
{
    private readonly MediaSessionRegistry _sessions = sessions;

    public StreamingLanStatus? GetStatus(Guid sessionId)
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

    public StreamingAsset? ResolveAsset(Guid sessionId, string? asset)
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

    public string? BuildWatchPage(Guid sessionId)
    {
        if (!_sessions.TryGet(sessionId, out var session) || !session.LanEnabled) return null;
        return $$"""
        <!doctype html><html><head><meta charset="utf-8"><title>PublisherStudio stream</title>
        <style>html,body{margin:0;background:#050b16;color:#fff;font:16px system-ui;height:100%}main{display:grid;place-items:center;height:100%}section{text-align:center;max-width:44rem}code{color:#93c5fd}</style></head>
        <body><main><section><h1>{{WebUtility.HtmlEncode(session.Name)}}</h1><p>The LAN output is prepared. The renderer/encoder must announce its WebRTC or HLS ingest before playback starts.</p><p>Session <code>{{sessionId:D}}</code></p></section></main></body></html>
        """;
    }
}

public sealed class StreamingLanStatus
{
    public Guid SessionId { get; init; }
    public bool Enabled { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public string? BrowserUrl { get; init; }
    public string? HlsUrl { get; init; }
    public string? RtspUrl { get; init; }
    public string? AccessToken { get; init; }
}

public sealed record StreamingAsset(string Path, string ContentType);

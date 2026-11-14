using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Automation;

/// <summary>
/// Coordinates user input automation behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="options">Browser automation configuration.</param>
public sealed class UserInputAutomationService(
    ILogger<UserInputAutomationService> logger,
    IOptions<OrganicPluginOptions> options) : IUserInputAutomationService
{
    /// <summary>
    /// Stores the in-memory commands collection maintained internally by <see cref="UserInputAutomationService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, BrowserAutomationCommand> _commands = new();
    private readonly Dictionary<Guid, DateTimeOffset> _tabLastSeen = new();
    private readonly object _gate = new();

    /// <summary>
    /// Performs enqueue as part of the user input automation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="command">Command value supplied to the user input automation operation and used when producing its result.</param>
    /// <returns>The browser automation command produced by the operation.</returns>
    public BrowserAutomationCommand Enqueue(BrowserAutomationCommand command)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(command);
            if (command.Kind == BrowserAutomationCommandKind.BrushStroke)
            {
                if (command.Selector != "#picture-studio-canvas")
                    throw new InvalidOperationException("BrushStroke requires the #picture-studio-canvas selector.");
                if (command.Points is null || command.Points.Count < 2 ||
                    command.Points.Count > options.Value.BrowserAutomationMaximumBrushPoints)
                    throw new InvalidOperationException("BrushStroke requires at least two points within OrganicPlugins:BrowserAutomationMaximumBrushPoints.");
                if (command.Points.Any(point => point is null || !double.IsFinite(point.X) || !double.IsFinite(point.Y) ||
                    point.X < 0 || point.X >= 1 || point.Y < 0 || point.Y >= 1))
                    throw new InvalidOperationException("BrushStroke point coordinates must be finite and in [0, 1).");
            }
            lock (_gate)
            {
                var now = DateTimeOffset.UtcNow;
                ExpireTabs(now);
                var tabId = command.TabId ?? SelectOnlyEnabledTab();
                if (!_tabLastSeen.ContainsKey(tabId))
                    throw new InvalidOperationException("The requested tabId does not match a live PublisherStudio tab with browser automation enabled.");

                command.TabId = tabId;
                command.CreatedUtc = now;
                command.CompletedUtc = null;
                command.Status = AutomationRequestStatus.Pending;
                command.Result = string.Empty;
                command.Error = string.Empty;
                if (command.Id == Guid.Empty) command.Id = Guid.NewGuid();
                if (!_commands.TryAdd(command.Id, command))
                    throw new InvalidOperationException("This browser input command id has already been queued.");
                TrimCompleted();
            }
            logger.LogInformation("Queued browser input command {CommandId} ({Kind}) for tab {TabId}.", command.Id, command.Kind, command.TabId);
            return command;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserInputAutomationService)}.{nameof(Enqueue)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserInputAutomationService)}.{nameof(Enqueue)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves all as part of the user input automation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<BrowserAutomationCommand> GetAll() {
    try
    {
        lock (_gate)
        {
            ExpireTabs(DateTimeOffset.UtcNow);
            return _commands.Values.OrderByDescending(command => command.CreatedUtc).Take(500).ToList().AsReadOnly();
        }
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserInputAutomationService)}.{nameof(GetAll)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserInputAutomationService)}.{nameof(GetAll)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs claim pending as part of the user input automation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="tabId">Browser document asking for commands bound to it.</param>
    /// <param name="maximum">Maximum value supplied to the user input automation operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<BrowserAutomationCommand> ClaimPending(Guid tabId, int maximum = 25)
    {
    try
    {
            if (tabId == Guid.Empty) return Array.Empty<BrowserAutomationCommand>();
            lock (_gate)
            {
                var now = DateTimeOffset.UtcNow;
                ExpireTabs(now);
                _tabLastSeen[tabId] = now;
                var claimed = _commands.Values
                    .Where(command => command.TabId == tabId && command.Status == AutomationRequestStatus.Pending)
                    .OrderBy(command => command.CreatedUtc)
                    .Take(Math.Clamp(maximum, 1, 100))
                    .ToList();
                foreach (var command in claimed) command.Status = AutomationRequestStatus.Claimed;
                if (claimed.Count > 0)
                    logger.LogDebug("Claimed {Count} browser input command(s) for tab {TabId}.", claimed.Count, tabId);
                return claimed.AsReadOnly();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserInputAutomationService)}.{nameof(ClaimPending)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserInputAutomationService)}.{nameof(ClaimPending)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs complete as part of the user input automation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="tabId">Browser document completing its own claimed command.</param>
    /// <param name="completion">Completion value supplied to the user input automation operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Complete(Guid id, Guid tabId, AutomationCompletion completion)
    {
    try
    {
            lock (_gate)
            {
                if (!_commands.TryGetValue(id, out var command) || command.TabId != tabId ||
                    command.Status != AutomationRequestStatus.Claimed)
                    return false;
                command.Result = completion.Result ?? string.Empty;
                command.Error = completion.Error ?? string.Empty;
                command.Status = string.IsNullOrWhiteSpace(command.Error) ? AutomationRequestStatus.Completed : AutomationRequestStatus.Failed;
                command.CompletedUtc = DateTimeOffset.UtcNow;
                if (command.Status == AutomationRequestStatus.Failed) logger.LogWarning("Browser input command {CommandId} failed: {Error}", id, command.Error);
                else logger.LogInformation("Browser input command {CommandId} completed.", id);
                return true;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserInputAutomationService)}.{nameof(Complete)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserInputAutomationService)}.{nameof(Complete)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether cel as part of the user input automation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Cancel(Guid id)
    {
    try
    {
            lock (_gate)
            {
                if (!_commands.TryGetValue(id, out var command) || command.Status != AutomationRequestStatus.Pending)
                    return false;
                command.Status = AutomationRequestStatus.Cancelled;
                command.CompletedUtc = DateTimeOffset.UtcNow;
                logger.LogInformation("Browser input command {CommandId} was cancelled.", id);
                return true;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserInputAutomationService)}.{nameof(Cancel)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserInputAutomationService)}.{nameof(Cancel)} failed.");
        throw;
    }
}

    /// <summary>Resolves implicit routing only when one live browser document has enabled automation.</summary>
    private Guid SelectOnlyEnabledTab()
    {
        try
        {
            return _tabLastSeen.Count switch
            {
                0 => throw new InvalidOperationException("Enable browser automation in the intended PublisherStudio tab and retry."),
                1 => _tabLastSeen.Keys.Single(),
                _ => throw new InvalidOperationException("Multiple PublisherStudio tabs have browser automation enabled. Pass the tabId from the earlier inspection, or enable automation only in the intended tab.")
            };
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Browser automation could not select a single enabled tab.");
            throw;
        }
    }

    /// <summary>Expires disconnected tabs without transferring their commands to another document.</summary>
    private void ExpireTabs(DateTimeOffset now)
    {
        try
        {
            var cutoff = now.AddSeconds(-Math.Max(1, options.Value.BrowserAutomationTabExpirySeconds));
            var expired = _tabLastSeen.Where(tab => tab.Value < cutoff).Select(tab => tab.Key).ToHashSet();
            if (expired.Count == 0) return;

            foreach (var tabId in expired) _tabLastSeen.Remove(tabId);
            foreach (var command in _commands.Values)
            {
                if (command.TabId is not { } tabId || !expired.Contains(tabId) ||
                    command.Status is not (AutomationRequestStatus.Pending or AutomationRequestStatus.Claimed))
                    continue;

                command.Error = "The PublisherStudio browser tab stopped polling within OrganicPlugins:BrowserAutomationTabExpirySeconds. Inspect the intended tab again before retrying.";
                command.CompletedUtc = now;
                command.Status = AutomationRequestStatus.Failed;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Expiring inactive browser automation tabs failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs trim completed as part of the user input automation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void TrimCompleted()
    {
    try
    {
            var expired = _commands.Values
                .Where(command => command.CompletedUtc < DateTimeOffset.UtcNow.AddHours(-2))
                .OrderBy(command => command.CompletedUtc)
                .Take(Math.Max(0, _commands.Count - 500))
                .Select(command => command.Id)
                .ToList();
            foreach (var id in expired) _commands.TryRemove(id, out _);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(UserInputAutomationService)}.{nameof(TrimCompleted)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(UserInputAutomationService)}.{nameof(TrimCompleted)} failed.");
        throw;
    }
}
}

/// <summary>
/// Coordinates screenshot capture behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class ScreenshotCaptureService(ILogger<ScreenshotCaptureService> logger) : IScreenshotCaptureService
{
    /// <summary>
    /// Stores the in-memory requests collection maintained internally by <see cref="ScreenshotCaptureService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, BrowserScreenshotRequest> _requests = new();

    /// <summary>
    /// Performs enqueue as part of the screenshot capture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The browser screenshot request produced by the operation.</returns>
    public BrowserScreenshotRequest Enqueue(BrowserScreenshotRequest request)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(request);
            request.Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id;
            request.CreatedUtc = DateTimeOffset.UtcNow;
            request.Status = AutomationRequestStatus.Pending;
            request.DataUrl = string.Empty;
            _requests[request.Id] = request;
            logger.LogInformation("Queued screenshot request {RequestId} for selector {Selector} at scale {Scale}.", request.Id, request.Selector, request.Scale);
            return request;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ScreenshotCaptureService)}.{nameof(Enqueue)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ScreenshotCaptureService)}.{nameof(Enqueue)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves all as part of the screenshot capture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<BrowserScreenshotRequest> GetAll() {
    try
    {
        return _requests.Values.OrderByDescending(request => request.CreatedUtc).Take(100).ToList().AsReadOnly();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ScreenshotCaptureService)}.{nameof(GetAll)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ScreenshotCaptureService)}.{nameof(GetAll)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs claim pending as part of the screenshot capture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="maximum">Maximum value supplied to the screenshot capture operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<BrowserScreenshotRequest> ClaimPending(int maximum = 5)
    {
    try
    {
            var claimed = new List<BrowserScreenshotRequest>();
            foreach (var request in _requests.Values.Where(request => request.Status == AutomationRequestStatus.Pending).OrderBy(request => request.CreatedUtc).Take(Math.Clamp(maximum, 1, 20)))
            {
                if (_requests.TryGetValue(request.Id, out var current) && current.Status == AutomationRequestStatus.Pending)
                {
                    current.Status = AutomationRequestStatus.Claimed;
                    claimed.Add(current);
                }
            }
            if (claimed.Count > 0) logger.LogDebug("Claimed {Count} screenshot request(s).", claimed.Count);
            return claimed.AsReadOnly();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ScreenshotCaptureService)}.{nameof(ClaimPending)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ScreenshotCaptureService)}.{nameof(ClaimPending)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs complete as part of the screenshot capture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="completion">Completion value supplied to the screenshot capture operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Complete(Guid id, ScreenshotCompletion completion)
    {
    try
    {
            if (!_requests.TryGetValue(id, out var request)) return false;
            request.DataUrl = completion.DataUrl ?? string.Empty;
            request.PixelWidth = completion.PixelWidth;
            request.PixelHeight = completion.PixelHeight;
            request.Error = completion.Error ?? string.Empty;
            request.Status = string.IsNullOrWhiteSpace(request.Error) ? AutomationRequestStatus.Completed : AutomationRequestStatus.Failed;
            request.CompletedUtc = DateTimeOffset.UtcNow;
            if (request.Status == AutomationRequestStatus.Failed) logger.LogWarning("Screenshot request {RequestId} failed: {Error}", id, request.Error);
            else logger.LogInformation("Screenshot request {RequestId} completed at {Width}x{Height}.", id, request.PixelWidth, request.PixelHeight);
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ScreenshotCaptureService)}.{nameof(Complete)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ScreenshotCaptureService)}.{nameof(Complete)} failed.");
        throw;
    }
}

    /// <summary>
    /// Attempts to get as part of the screenshot capture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool TryGet(Guid id, out BrowserScreenshotRequest request) {
    try
    {
        return _requests.TryGetValue(id, out request!);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ScreenshotCaptureService)}.{nameof(TryGet)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ScreenshotCaptureService)}.{nameof(TryGet)} failed.");
        throw;
    }
}

    /// <summary>
    /// Determines whether cel as part of the screenshot capture service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Cancel(Guid id)
    {
    try
    {
            if (!_requests.TryGetValue(id, out var request)) return false;
            request.Status = AutomationRequestStatus.Cancelled;
            request.CompletedUtc = DateTimeOffset.UtcNow;
            logger.LogInformation("Screenshot request {RequestId} was cancelled.", id);
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ScreenshotCaptureService)}.{nameof(Cancel)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ScreenshotCaptureService)}.{nameof(Cancel)} failed.");
        throw;
    }
}
}

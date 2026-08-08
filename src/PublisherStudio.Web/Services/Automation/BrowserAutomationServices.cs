using System.Collections.Concurrent;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Automation;

/// <summary>
/// Provides user input automation service operations.
/// </summary>
public sealed class UserInputAutomationService(ILogger<UserInputAutomationService> logger) : IUserInputAutomationService
{
    private readonly ConcurrentDictionary<Guid, BrowserAutomationCommand> _commands = new();

    /// <summary>
    /// Runs the enqueue operation.
    /// </summary>
    public BrowserAutomationCommand Enqueue(BrowserAutomationCommand command)
    {
    try
    {
            ArgumentNullException.ThrowIfNull(command);
            command.Id = command.Id == Guid.Empty ? Guid.NewGuid() : command.Id;
            command.CreatedUtc = DateTimeOffset.UtcNow;
            command.Status = AutomationRequestStatus.Pending;
            _commands[command.Id] = command;
            logger.LogInformation("Queued browser input command {CommandId} ({Kind}) for selector {Selector}.", command.Id, command.Kind, command.Selector);
            TrimCompleted();
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
    /// Gets all.
    /// </summary>
    public IReadOnlyList<BrowserAutomationCommand> GetAll() {
    try
    {
        return _commands.Values.OrderByDescending(command => command.CreatedUtc).Take(500).ToList().AsReadOnly();
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
    /// Runs the claim pending operation.
    /// </summary>
    public IReadOnlyList<BrowserAutomationCommand> ClaimPending(int maximum = 25)
    {
    try
    {
            var claimed = new List<BrowserAutomationCommand>();
            foreach (var command in _commands.Values.Where(command => command.Status == AutomationRequestStatus.Pending).OrderBy(command => command.CreatedUtc).Take(Math.Clamp(maximum, 1, 100)))
            {
                if (_commands.TryGetValue(command.Id, out var current) && current.Status == AutomationRequestStatus.Pending)
                {
                    current.Status = AutomationRequestStatus.Claimed;
                    claimed.Add(current);
                }
            }
            if (claimed.Count > 0) logger.LogDebug("Claimed {Count} browser input command(s).", claimed.Count);
            return claimed.AsReadOnly();
    
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
    /// Runs the complete operation.
    /// </summary>
    public bool Complete(Guid id, AutomationCompletion completion)
    {
    try
    {
            if (!_commands.TryGetValue(id, out var command)) return false;
            command.Result = completion.Result ?? string.Empty;
            command.Error = completion.Error ?? string.Empty;
            command.Status = string.IsNullOrWhiteSpace(command.Error) ? AutomationRequestStatus.Completed : AutomationRequestStatus.Failed;
            command.CompletedUtc = DateTimeOffset.UtcNow;
            if (command.Status == AutomationRequestStatus.Failed) logger.LogWarning("Browser input command {CommandId} failed: {Error}", id, command.Error);
            else logger.LogInformation("Browser input command {CommandId} completed.", id);
            return true;
    
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
    /// Determines whether cel.
    /// </summary>
    public bool Cancel(Guid id)
    {
    try
    {
            if (!_commands.TryGetValue(id, out var command)) return false;
            command.Status = AutomationRequestStatus.Cancelled;
            command.CompletedUtc = DateTimeOffset.UtcNow;
            logger.LogInformation("Browser input command {CommandId} was cancelled.", id);
            return true;
    
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
/// Provides screenshot capture service operations.
/// </summary>
public sealed class ScreenshotCaptureService(ILogger<ScreenshotCaptureService> logger) : IScreenshotCaptureService
{
    private readonly ConcurrentDictionary<Guid, BrowserScreenshotRequest> _requests = new();

    /// <summary>
    /// Runs the enqueue operation.
    /// </summary>
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
    /// Gets all.
    /// </summary>
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
    /// Runs the claim pending operation.
    /// </summary>
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
    /// Runs the complete operation.
    /// </summary>
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
    /// Attempts to get.
    /// </summary>
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
    /// Determines whether cel.
    /// </summary>
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

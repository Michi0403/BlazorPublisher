using System.Collections.Concurrent;
using PublisherStudio.Domain;

namespace PublisherStudio.Services.Automation;

public sealed class UserInputAutomationService(ILogger<UserInputAutomationService> logger) : IUserInputAutomationService
{
    private readonly ConcurrentDictionary<Guid, BrowserAutomationCommand> _commands = new();

    public BrowserAutomationCommand Enqueue(BrowserAutomationCommand command)
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

    public IReadOnlyList<BrowserAutomationCommand> GetAll() =>
        _commands.Values.OrderByDescending(command => command.CreatedUtc).Take(500).ToList().AsReadOnly();

    public IReadOnlyList<BrowserAutomationCommand> ClaimPending(int maximum = 25)
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

    public bool Complete(Guid id, AutomationCompletion completion)
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

    public bool Cancel(Guid id)
    {
        if (!_commands.TryGetValue(id, out var command)) return false;
        command.Status = AutomationRequestStatus.Cancelled;
        command.CompletedUtc = DateTimeOffset.UtcNow;
        logger.LogInformation("Browser input command {CommandId} was cancelled.", id);
        return true;
    }

    private void TrimCompleted()
    {
        var expired = _commands.Values
            .Where(command => command.CompletedUtc < DateTimeOffset.UtcNow.AddHours(-2))
            .OrderBy(command => command.CompletedUtc)
            .Take(Math.Max(0, _commands.Count - 500))
            .Select(command => command.Id)
            .ToList();
        foreach (var id in expired) _commands.TryRemove(id, out _);
    }
}

public sealed class ScreenshotCaptureService(ILogger<ScreenshotCaptureService> logger) : IScreenshotCaptureService
{
    private readonly ConcurrentDictionary<Guid, BrowserScreenshotRequest> _requests = new();

    public BrowserScreenshotRequest Enqueue(BrowserScreenshotRequest request)
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

    public IReadOnlyList<BrowserScreenshotRequest> GetAll() =>
        _requests.Values.OrderByDescending(request => request.CreatedUtc).Take(100).ToList().AsReadOnly();

    public IReadOnlyList<BrowserScreenshotRequest> ClaimPending(int maximum = 5)
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

    public bool Complete(Guid id, ScreenshotCompletion completion)
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

    public bool TryGet(Guid id, out BrowserScreenshotRequest request) => _requests.TryGetValue(id, out request!);

    public bool Cancel(Guid id)
    {
        if (!_requests.TryGetValue(id, out var request)) return false;
        request.Status = AutomationRequestStatus.Cancelled;
        request.CompletedUtc = DateTimeOffset.UtcNow;
        logger.LogInformation("Screenshot request {RequestId} was cancelled.", id);
        return true;
    }
}

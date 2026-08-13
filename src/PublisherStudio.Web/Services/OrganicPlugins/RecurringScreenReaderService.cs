using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Automation;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// User-started, single-flight screen-reader sessions. A session never queues a new screenshot or LocalGPT
/// request while the previous evidence package is still pending, which prevents timer pile-ups and race conditions.
/// </summary>
/// <param name="codec">Organic plugin protocol codec dependency used by the recurring screen reader workflow to provide the corresponding application capability.</param>
/// <param name="screenshots">Screenshot capture service dependency used by the recurring screen reader workflow to provide the corresponding application capability.</param>
/// <param name="services">Service provider dependency used by the recurring screen reader workflow to provide the corresponding application capability.</param>
/// <param name="options">Options containing the caller-supplied values that control this operation.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class RecurringScreenReaderService(
    IOrganicPluginProtocolCodec codec,
    IScreenshotCaptureService screenshots,
    IServiceProvider services,
    IOptions<OrganicPluginOptions> options,
    ILogger<RecurringScreenReaderService> logger) : IRecurringScreenReaderService, IAsyncDisposable
{
    /// <summary>
    /// Represents a runtime helper type nested within <see cref="RecurringScreenReaderService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="Session">Session value supplied to the recurring screen reader operation and used when producing its result.</param>
    /// <param name="Cancellation">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="Loop">Loop value supplied to the recurring screen reader operation and used when producing its result.</param>
    private sealed record Runtime(RecurringScreenReaderSession Session, CancellationTokenSource Cancellation, Task Loop);
    /// <summary>
    /// Stores the in-memory runtimes collection maintained internally by <see cref="RecurringScreenReaderService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, Runtime> runtimes = new();
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="RecurringScreenReaderService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Retrieves sessions as part of the recurring screen reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<RecurringScreenReaderSession> GetSessions() {
    try
    {
        return runtimes.Values
        .Select(runtime => runtime.Session)
        .OrderByDescending(session => session.CreatedUtc)
        .ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RecurringScreenReaderService)}.{nameof(GetSessions)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RecurringScreenReaderService)}.{nameof(GetSessions)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs start as part of the recurring screen reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="selector">Selector value supplied to the recurring screen reader operation and used when producing its result.</param>
    /// <param name="prompt">Prompt value supplied to the recurring screen reader operation and used when producing its result.</param>
    /// <param name="intervalSeconds">Interval seconds value supplied to the recurring screen reader operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The recurring screen reader session produced by the operation.</returns>
    public Task<RecurringScreenReaderSession> StartAsync(
        string peerId,
        string selector,
        string prompt,
        int intervalSeconds,
        CancellationToken cancellationToken = default)
    {
    try
    {
            var connection = services.GetRequiredService<ILocalGptConnectionService>();
            if (!connection.State.IsConnected)
                throw new InvalidOperationException("PublisherStudio must be connected to LocalGPT before recurring screen-reader help can start.");
            if (!string.IsNullOrWhiteSpace(peerId) && !string.Equals(peerId, connection.State.PeerId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The requested LocalGPT peer is not the currently connected peer.");

            var minimum = Math.Max(15, options.Value.MinimumRecurringScreenReaderIntervalSeconds);
            var session = new RecurringScreenReaderSession
            {
                PeerId = connection.State.PeerId,
                Selector = string.IsNullOrWhiteSpace(selector) ? "body" : selector.Trim(),
                Prompt = string.IsNullOrWhiteSpace(prompt)
                    ? "Describe meaningful screen changes and suggest the next safe action."
                    : prompt.Trim(),
                IntervalSeconds = Math.Clamp(intervalSeconds, minimum, 3600),
                CorrelationId = Guid.NewGuid(),
                IsActive = true
            };
            var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var loop = RunAsync(session, linked.Token);
            if (!runtimes.TryAdd(session.Id, new Runtime(session, linked, loop)))
            {
                linked.Cancel();
                linked.Dispose();
                throw new InvalidOperationException("Could not register the recurring screen-reader session.");
            }
            Changed?.Invoke();
            logger.LogInformation("Started recurring screen-reader session {SessionId} every {IntervalSeconds}s for {Selector}.", session.Id, session.IntervalSeconds, session.Selector);
            return Task.FromResult(session);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RecurringScreenReaderService)}.{nameof(StartAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RecurringScreenReaderService)}.{nameof(StartAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs stop as part of the recurring screen reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public async Task<bool> StopAsync(Guid sessionId)
    {
        if (!runtimes.TryRemove(sessionId, out var runtime))
            return false;
        runtime.Session.IsActive = false;
        runtime.Cancellation.Cancel();
        try { await runtime.Loop.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        runtime.Cancellation.Dispose();
        if (runtime.Session.ActiveScreenshotRequestId is { } screenshotId)
            screenshots.Cancel(screenshotId);
        Changed?.Invoke();
        logger.LogInformation("Stopped recurring screen-reader session {SessionId}.", sessionId);
        return true;
    }

    /// <summary>
    /// Performs run as part of the recurring screen reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the recurring screen reader operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RunAsync(RecurringScreenReaderSession session, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && session.IsActive)
            {
                var started = DateTimeOffset.UtcNow;
                try
                {
                    await ExecuteSingleFlightAsync(session, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    session.LastError = ex.Message;
                    logger.LogWarning(ex, "Recurring screen-reader session {SessionId} iteration failed.", session.Id);
                }
                finally
                {
                    Changed?.Invoke();
                }

                var elapsed = DateTimeOffset.UtcNow - started;
                var delay = TimeSpan.FromSeconds(session.IntervalSeconds) - elapsed;
                if (delay < TimeSpan.FromMilliseconds(250))
                    delay = TimeSpan.FromMilliseconds(250);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            session.IsActive = false;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Executes single flight as part of the recurring screen reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="session">Session value supplied to the recurring screen reader operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ExecuteSingleFlightAsync(RecurringScreenReaderSession session, CancellationToken cancellationToken)
    {
    try
    {
            // The sequential session loop itself is the debounce/single-flight gate: no second package is created
            // until browser capture and the corresponding LocalGPT response have both completed.
            var request = screenshots.Enqueue(new BrowserScreenshotRequest
            {
                Selector = session.Selector,
                Format = "jpeg",
                Quality = .72,
                Scale = .75,
                IncludeMetadata = true
            });
            session.ActiveScreenshotRequestId = request.Id;
            session.LastQueuedUtc = DateTimeOffset.UtcNow;
            session.LastError = string.Empty;
            Changed?.Invoke();

            var completed = await WaitForScreenshotAsync(request.Id, TimeSpan.FromSeconds(45), cancellationToken).ConfigureAwait(false);
            if (completed.Status != AutomationRequestStatus.Completed)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(completed.Error) ? "The browser screenshot did not complete." : completed.Error);

            var inlineData = completed.DataUrl.Length <= 650_000 ? completed.DataUrl : string.Empty;
            var interaction = new
            {
                SessionId = session.Id,
                ScreenshotRequestId = completed.Id,
                session.Selector,
                session.Prompt,
                completed.PixelWidth,
                completed.PixelHeight,
                DataUrl = inlineData,
                DataIncluded = inlineData.Length > 0,
                CapturedUtc = completed.CompletedUtc,
                Debounce = new { MinimumIntervalSeconds = session.IntervalSeconds, MaximumPendingExecutions = 1 }
            };
            var interactionJson = JsonSerializer.Serialize(interaction, codec.JsonOptions);
            var envelope = new OrganicWireEnvelope
            {
                MessageType = OrganicWireMessageType.Invoke,
                CorrelationId = Guid.NewGuid(),
                CapabilityKey = "localgpt.screenreader.help",
                Controller = "OneWire",
                Method = "POST",
                Route = "/api/onewire/screenreader/help",
                Organs = ["eyes", "brain"],
                Skills = ["vision", "screenreader"],
                ExecutionMode = OrganicExecutionMode.Recurring,
                WorkOrderKey = $"screenreader:{session.Id:N}",
                UserConfirmed = true,
                RequiresAutomatedInteractionOnTargetSystem = true,
                InteractionValueJson = interactionJson,
                InteractionValueContentType = "application/json",
                Properties = new Dictionary<string, JsonElement>
                {
                    ["Parameters"] = JsonSerializer.SerializeToElement(interaction, codec.JsonOptions),
                    ["Recurring"] = JsonSerializer.SerializeToElement(new OrganicRecurringExecution
                    {
                        IntervalSeconds = session.IntervalSeconds,
                        DebounceMilliseconds = 750,
                        MaximumPendingExecutions = 1
                    }, codec.JsonOptions)
                }
            };
            session.CorrelationId = envelope.CorrelationId;
            var connection = services.GetRequiredService<ILocalGptConnectionService>();
            if (!connection.State.IsConnected || !string.Equals(connection.State.PeerId, session.PeerId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The LocalGPT peer for this recurring screen-reader session is no longer connected.");
            await connection.SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
            var response = await connection.WaitForResultAsync(envelope.CorrelationId, TimeSpan.FromMinutes(3), cancellationToken).ConfigureAwait(false);
            session.LastResultJson = response.Properties is null
                ? response.InteractionValueJson ?? string.Empty
                : JsonSerializer.Serialize(response.Properties, codec.JsonOptions);
            session.LastError = response.Error;
            session.LastCompletedUtc = DateTimeOffset.UtcNow;
            session.ActiveScreenshotRequestId = null;
            session.CompletedExecutions++;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RecurringScreenReaderService)}.{nameof(ExecuteSingleFlightAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RecurringScreenReaderService)}.{nameof(ExecuteSingleFlightAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs wait for screenshot as part of the recurring screen reader service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="requestId">Identifier of the request to use for this operation.</param>
    /// <param name="timeout">Timeout value supplied to the recurring screen reader operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The browser screenshot request produced by the operation.</returns>
    private async Task<BrowserScreenshotRequest> WaitForScreenshotAsync(Guid requestId, TimeSpan timeout, CancellationToken cancellationToken)
    {
    try
    {
            var deadline = DateTimeOffset.UtcNow + timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!screenshots.TryGet(requestId, out var current))
                    throw new KeyNotFoundException("The queued screenshot request disappeared before completion.");
                if (current.Status is AutomationRequestStatus.Completed or AutomationRequestStatus.Failed or AutomationRequestStatus.Cancelled)
                    return current;
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }
            screenshots.Cancel(requestId);
            throw new TimeoutException("The browser did not return the screenshot within 45 seconds. Ensure the PublisherStudio tab is open and screen capture is permitted.");
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RecurringScreenReaderService)}.{nameof(WaitForScreenshotAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RecurringScreenReaderService)}.{nameof(WaitForScreenshotAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Releases resources owned by <see cref="RecurringScreenReaderService"/> and leaves the recurring screen reader workflow in a safely disposed state.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async ValueTask DisposeAsync()
    {
    try
    {
            foreach (var id in runtimes.Keys.ToArray())
                await StopAsync(id).ConfigureAwait(false);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(RecurringScreenReaderService)}.{nameof(DisposeAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(RecurringScreenReaderService)}.{nameof(DisposeAsync)} failed.");
        throw;
    }
}
}

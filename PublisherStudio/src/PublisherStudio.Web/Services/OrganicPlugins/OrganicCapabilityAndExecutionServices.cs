using PublisherStudio.Domain;
using PublisherStudio.Services;
using PublisherStudio.Services.Automation;
using PublisherStudio.Services.MediaConversion;
using PublisherStudio.Services.OpenScad;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

public sealed class OrganicCapabilityCatalog(
    IMediaConversionService mediaConversion,
    ILogger<OrganicCapabilityCatalog> logger) : IOrganicCapabilityCatalog
{
    public async Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var media = await mediaConversion.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
        var capabilities = new List<OrganicCapabilityDescriptor>
        {
            Capability("publisher.screen.capture", "Eyes: screen capture", "Queues a browser screenshot. Browser/user gesture permission is still required.", "eyes", false, true, true),
            Capability("publisher.screen.capture.result", "Eyes: screen capture result", "Reads the bounded status and result of a previously queued browser screenshot for the next council heartbeat.", "eyes", true, false, true),
            Capability("publisher.screenreader.start", "Start recurring screen-reader help", "Starts one debounced single-flight screen-reader session. The interval is clamped to at least 15 seconds.", "eyes", false, true, true),
            Capability("publisher.screenreader.stop", "Stop recurring screen-reader help", "Stops a recurring screen-reader session by id.", "eyes", false, true, true),
            Capability("publisher.input.execute", "Hands: browser input", "Queues one bounded mouse, keyboard, hover, focus or gesture command through the existing automation service.", "hands", false, true, true),
            Capability("publisher.input.result", "Hands: browser input result", "Reads the status and bounded result of a previously queued browser input command.", "eyes", true, false, true),
            Capability("publisher.openscad.generate", "OpenSCAD canonical project generation", "Validates and renders a canonical OpenScadDocument/OpenScadNode graph through the existing registered renderer path.", "hands", false, true, false),
            Capability("publisher.spreadsheet.inspect", "Spreadsheet session inspection", "Returns bounded metadata and preview evidence for an existing workbook session without mutation.", "eyes", true, true, true),
            Capability("publisher.text.insert.propose", "Propose text insertion", "Creates a reviewable text insertion proposal. It never mutates a publication automatically.", "hands", false, true, true),
            Capability("publisher.business-context", "PublisherStudio project/API context", "Returns the current domain, service and controller context for grounded council planning.", "eyes", true, false, false),
            Capability("publisher.media.capabilities", "FFmpeg/media capabilities", $"Returns the installed PublisherStudio media conversion capability map. FFmpeg available: {media.Available}.", "eyes", true, false, false)
        };
        logger.LogDebug("Published {CapabilityCount} PublisherStudio organic capabilities.", capabilities.Count);
        return capabilities;
    }

    public async Task<IReadOnlyList<OrganicSkillDescriptor>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = await GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
        return capabilities
            .SelectMany(capability => capability.Skills.Select(skill => new { skill, capability }))
            .GroupBy(item => item.skill, StringComparer.OrdinalIgnoreCase)
            .Select(group => new OrganicSkillDescriptor
            {
                Key = group.Key,
                DisplayName = group.Key,
                Description = $"PublisherStudio organic skill backed by {group.Select(item => item.capability.Key).Distinct().Count()} capability route(s).",
                SourcePeerId = "publisherstudio",
                Organs = group.SelectMany(item => item.capability.Organs).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                CapabilityKeys = group.Select(item => item.capability.Key).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                UiActivationKeys = group.SelectMany(item => item.capability.UiActivationKeys).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                IsOnline = group.Any(item => item.capability.IsOnline),
                IsEnabled = group.Any(item => item.capability.IsEnabled),
                UpdatedUtc = DateTimeOffset.UtcNow
            })
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<IReadOnlyList<OrganicUiFeatureDescriptor>> GetUiFeaturesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganicUiFeatureDescriptor>>
        ([
            new() { Key = "publisherstudio.council.run", DisplayName = "AI Council ribbon", State = OrganicUiFeatureState.Enabled, RequiredCapabilityKeys = ["council.run"] },
            new() { Key = "publisherstudio.council.team-picker", DisplayName = "Council team picker", State = OrganicUiFeatureState.Enabled, RequiredCapabilityKeys = ["council.teams"] },
            new() { Key = "publisherstudio.screenreader.recurring", DisplayName = "Recurring screen-reader help", State = OrganicUiFeatureState.Enabled, RequiredCapabilityKeys = ["publisher.screen.capture"] },
            new() { Key = "publisherstudio.spreadsheet.ai", DisplayName = "Spreadsheet AI help", State = OrganicUiFeatureState.Enabled, RequiredCapabilityKeys = ["publisher.spreadsheet.inspect", "council.run"] },
            new() { Key = "publisherstudio.openscad.ai", DisplayName = "OpenSCAD Team", State = OrganicUiFeatureState.Enabled, RequiredCapabilityKeys = ["publisher.openscad.generate", "council.run"] }
        ]);

    public Task<IReadOnlyList<OrganicHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganicHardwareDescriptor>>
        ([
            new()
            {
                Kind = OrganicHardwareKind.Cpu, Index = 0, Name = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "CPU",
                Vendor = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? string.Empty,
                LogicalProcessorCount = Environment.ProcessorCount, IsOnline = true
            }
        ]);

    private static OrganicCapabilityDescriptor Capability(string key, string name, string description, string organ, bool readOnly, bool confirmation, bool scheduling) => new()
    {
        Key = key, DisplayName = name, Description = description, Controller = "OrganicPlugins", Method = "POST",
        Route = $"/api/organic/capabilities/{key}", Organs = [organ], Skills = Skills(key), RequiredSkillKeys = Skills(key),
        UiActivationKeys = UiActivationKeys(key), IsOnline = true, IsEnabled = true,
        IsReadOnly = readOnly, RequiresHumanConfirmation = confirmation, SupportsScheduling = scheduling,
        SupportsRecurringExecution = key is "publisher.screen.capture" or "publisher.screenreader.start",
        RequiresHumanInteractionOnTargetSystem = confirmation,
        RequiresAutomatedInteractionOnTargetSystem = key is "publisher.screen.capture" or "publisher.input.execute" or "publisher.screenreader.start" or "publisher.screenreader.stop",
        IsExposedToPeer = true,
        AllowPeerInvocation = true,
        RequiresFrontendUserConfirmation = confirmation,
        InteractionEditor = key == "publisher.text.insert.propose"
            ? OrganicInteractionEditor.RichText
            : confirmation
                ? OrganicInteractionEditor.ConfirmationOnly
                : OrganicInteractionEditor.None,
        ConfigurationKey = $"publisher:{key}",
        ParameterSchemaJson = Schema(key)
    };

    private static List<string> UiActivationKeys(string key) => key switch
    {
        "publisher.screen.capture" => ["publisherstudio.screenreader.recurring"],
        "publisher.screenreader.start" => ["publisherstudio.screenreader.recurring"],
        "publisher.screenreader.stop" => ["publisherstudio.screenreader.recurring"],
        "publisher.openscad.generate" => ["publisherstudio.openscad.ai"],
        "publisher.spreadsheet.inspect" => ["publisherstudio.spreadsheet.ai"],
        _ => []
    };

    private static List<string> Skills(string key) => key switch
    {
        "publisher.screen.capture" => ["vision", "screen", "screenshot"],
        "publisher.screen.capture.result" => ["vision", "screen", "screenshot", "evidence"],
        "publisher.screenreader.start" => ["vision", "screen", "screenreader", "recurring"],
        "publisher.screenreader.stop" => ["vision", "screen", "screenreader", "recurring"],
        "publisher.input.execute" => ["mouse", "keyboard", "gesture", "navigation"],
        "publisher.input.result" => ["mouse", "keyboard", "gesture", "verification"],
        "publisher.openscad.generate" => ["openscad", "3d", "geometry", "render"],
        "publisher.spreadsheet.inspect" => ["spreadsheet", "workbook", "analysis"],
        "publisher.text.insert.propose" => ["text", "editing", "proposal"],
        "publisher.media.capabilities" => ["ffmpeg", "video", "audio", "conversion"],
        _ => ["publisherstudio", "project-context"]
    };

    private static string Schema(string key) => key switch
    {
        "publisher.screen.capture" => "{\"type\":\"object\",\"properties\":{\"selector\":{\"type\":\"string\"},\"format\":{\"type\":\"string\"},\"quality\":{\"type\":\"number\"}}}",
        "publisher.screen.capture.result" => "{\"type\":\"object\",\"required\":[\"requestId\"],\"properties\":{\"requestId\":{\"type\":\"string\"},\"includeData\":{\"type\":\"boolean\"}}}",
        "publisher.screenreader.start" => "{\"type\":\"object\",\"properties\":{\"selector\":{\"type\":\"string\"},\"prompt\":{\"type\":\"string\"},\"intervalSeconds\":{\"type\":\"integer\",\"minimum\":15}}}",
        "publisher.screenreader.stop" => "{\"type\":\"object\",\"required\":[\"sessionId\"],\"properties\":{\"sessionId\":{\"type\":\"string\"}}}",
        "publisher.input.execute" => "{\"type\":\"object\",\"required\":[\"kind\"],\"properties\":{\"kind\":{\"type\":\"string\"},\"selector\":{\"type\":\"string\"},\"text\":{\"type\":\"string\"},\"key\":{\"type\":\"string\"},\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"}}}",
        "publisher.input.result" => "{\"type\":\"object\",\"required\":[\"requestId\"],\"properties\":{\"requestId\":{\"type\":\"string\"}}}",
        "publisher.openscad.generate" => "{\"type\":\"object\",\"required\":[\"document\"],\"properties\":{\"document\":{\"type\":\"object\"}}}",
        "publisher.spreadsheet.inspect" => "{\"type\":\"object\",\"required\":[\"sessionId\"],\"properties\":{\"sessionId\":{\"type\":\"string\"}}}",
        "publisher.text.insert.propose" => "{\"type\":\"object\",\"required\":[\"text\"],\"properties\":{\"target\":{\"type\":\"string\"},\"text\":{\"type\":\"string\"},\"reason\":{\"type\":\"string\"}}}",
        _ => "{\"type\":\"object\",\"properties\":{}}"
    };
}

public sealed class OrganicWorkExecutor(
    IUserInputAutomationService input,
    IScreenshotCaptureService screenshots,
    IOpenScadDocumentService openScad,
    SpreadsheetSessionStore spreadsheetSessions,
    IBusinessObjectContextService businessContext,
    IMediaConversionService mediaConversion,
    IOrganicResultStore resultStore,
    IRecurringScreenReaderService recurringScreenReader,
    ILogger<OrganicWorkExecutor> logger) : IOrganicWorkExecutor
{
    public async Task<string> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var parameters = ReadParameters(envelope);
        object result;
        if (string.Equals(envelope.CapabilityKey, "publisher.screenreader.start", StringComparison.OrdinalIgnoreCase))
        {
            var session = await recurringScreenReader.StartAsync(
                envelope.SourcePeerId,
                GetString(parameters, "selector", "body"),
                GetString(parameters, "prompt", "Describe meaningful screen changes and suggest the next safe action."),
                GetInt(parameters, "intervalSeconds", 15),
                cancellationToken).ConfigureAwait(false);
            result = new { session.Id, session.IsActive, session.Selector, session.Prompt, session.IntervalSeconds, MinimumIntervalSeconds = 15 };
        }
        else if (string.Equals(envelope.CapabilityKey, "publisher.screenreader.stop", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(GetString(parameters, "sessionId", string.Empty), out var sessionId))
                throw new ArgumentException("sessionId is required.");
            result = new { SessionId = sessionId, Stopped = await recurringScreenReader.StopAsync(sessionId).ConfigureAwait(false) };
        }
        else
        {
            result = envelope.CapabilityKey switch
            {
                "publisher.screen.capture" => QueueScreenshot(parameters),
                "publisher.screen.capture.result" => ReadScreenshotResult(parameters),
                "publisher.input.execute" => QueueInput(parameters),
                "publisher.input.result" => ReadInputResult(parameters),
                "publisher.openscad.generate" => GenerateOpenScad(parameters),
                "publisher.spreadsheet.inspect" => InspectSpreadsheet(parameters),
                "publisher.text.insert.propose" => ProposeText(parameters),
                "publisher.business-context" => businessContext.CreateSnapshot(),
                "publisher.media.capabilities" => await mediaConversion.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false),
                _ => throw new KeyNotFoundException($"Unknown organic capability '{envelope.CapabilityKey}'.")
            };
        }
        logger.LogInformation("Executed organic capability {CapabilityKey} for correlation {CorrelationId}.", envelope.CapabilityKey, envelope.CorrelationId);
        return JsonSerializer.Serialize(result, OrganicPluginProtocolCodec.JsonOptions);
    }

    private object QueueScreenshot(JsonElement parameters)
    {
        var request = new BrowserScreenshotRequest
        {
            Selector = GetString(parameters, "selector", "body"),
            Format = GetString(parameters, "format", "png"),
            Quality = GetDouble(parameters, "quality", .92),
            Scale = GetDouble(parameters, "scale", 1),
            IncludeMetadata = GetBoolean(parameters, "includeMetadata", true)
        };
        var queued = screenshots.Enqueue(request);
        return new { queued.Id, queued.Status, RequiresBrowserUserGesture = true, NextCapability = "publisher.spreadsheet.inspect or council continuation" };
    }

    private object QueueInput(JsonElement parameters)
    {
        var kindName = GetString(parameters, "kind", string.Empty);
        if (!Enum.TryParse<BrowserAutomationCommandKind>(kindName, true, out var kind))
            throw new ArgumentException("A valid browser automation kind is required.");
        var command = new BrowserAutomationCommand
        {
            Kind = kind, Selector = GetString(parameters, "selector", string.Empty), Text = GetString(parameters, "text", string.Empty),
            Key = GetString(parameters, "key", string.Empty), Code = GetString(parameters, "code", string.Empty),
            Button = GetInt(parameters, "button", 0), X = GetDouble(parameters, "x", 0), Y = GetDouble(parameters, "y", 0),
            DeltaX = GetDouble(parameters, "deltaX", 0), DeltaY = GetDouble(parameters, "deltaY", 0),
            CtrlKey = GetBoolean(parameters, "ctrlKey", false), ShiftKey = GetBoolean(parameters, "shiftKey", false),
            AltKey = GetBoolean(parameters, "altKey", false), MetaKey = GetBoolean(parameters, "metaKey", false)
        };
        var queued = input.Enqueue(command);
        return new { queued.Id, queued.Status, GestureOwner = "PublisherStudio browser automation queue" };
    }

    private object ReadScreenshotResult(JsonElement parameters)
    {
        if (!Guid.TryParse(GetString(parameters, "requestId", string.Empty), out var requestId))
            throw new ArgumentException("requestId is required.");
        if (!screenshots.TryGet(requestId, out var request))
            throw new KeyNotFoundException("Screenshot request not found or expired.");
        var includeData = GetBoolean(parameters, "includeData", true);
        var dataUrl = includeData ? request.DataUrl : string.Empty;
        if (System.Text.Encoding.UTF8.GetByteCount(dataUrl) > OrganicWireProtocol.MaximumMessageBytes)
            throw new InvalidOperationException("The screenshot result exceeds the configured 1-Wire message capacity. Increase OrganicPlugins:MaximumMessageBytes or request metadata only.");
        return new
        {
            request.Id, request.Status, request.PixelWidth, request.PixelHeight, request.Error,
            DataUrl = dataUrl,
            DataIncluded = includeData && !string.IsNullOrWhiteSpace(dataUrl),
            ReadyForNextHeartbeat = request.Status is AutomationRequestStatus.Completed or AutomationRequestStatus.Failed or AutomationRequestStatus.Cancelled
        };
    }

    private object ReadInputResult(JsonElement parameters)
    {
        if (!Guid.TryParse(GetString(parameters, "requestId", string.Empty), out var requestId))
            throw new ArgumentException("requestId is required.");
        var command = input.GetAll().FirstOrDefault(candidate => candidate.Id == requestId)
            ?? throw new KeyNotFoundException("Browser input request not found or expired.");
        return new
        {
            command.Id, command.Kind, command.Selector, command.Status, command.Result, command.Error,
            ReadyForNextHeartbeat = command.Status is AutomationRequestStatus.Completed or AutomationRequestStatus.Failed or AutomationRequestStatus.Cancelled
        };
    }

    private object GenerateOpenScad(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("document", out var documentElement)) throw new ArgumentException("document is required.");
        var document = documentElement.Deserialize<OpenScadDocument>(OrganicPluginProtocolCodec.JsonOptions) ?? throw new ArgumentException("document is invalid.");
        return openScad.Generate(document);
    }

    private object InspectSpreadsheet(JsonElement parameters)
    {
        if (!Guid.TryParse(GetString(parameters, "sessionId", string.Empty), out var sessionId)) throw new ArgumentException("sessionId is required.");
        if (!spreadsheetSessions.TryGet(sessionId, out var session)) throw new KeyNotFoundException("Spreadsheet session not found or expired.");
        return new
        {
            session.Id, session.ElementId, session.DocumentId, session.FileName, session.SourceFormat, session.ActiveSheetName,
            ContentBytes = session.Content.Length,
            ContentSha256 = Convert.ToHexString(SHA256.HashData(session.Content)),
            PreviewHtml = session.PreviewHtml.Length <= 24000 ? session.PreviewHtml : session.PreviewHtml[..24000] + "…",
            ReadOnlyInspection = true
        };
    }

    private object ProposeText(JsonElement parameters)
    {
        var proposal = new OrganicTextInsertionProposal
        {
            Target = GetString(parameters, "target", "current selection"),
            Text = GetString(parameters, "text", string.Empty),
            Reason = GetString(parameters, "reason", "AI Council proposal")
        };
        if (string.IsNullOrWhiteSpace(proposal.Text)) throw new ArgumentException("text is required.");
        resultStore.AddTextProposal(proposal);
        return new { proposal.Id, proposal.Target, proposal.Text, proposal.Reason, RequiresUserAcceptance = true };
    }

    private static JsonElement ReadParameters(OrganicWireEnvelope envelope)
    {
        if (envelope.Properties is not null && envelope.Properties.TryGetValue("Parameters", out var parameters)) return parameters;
        return JsonSerializer.SerializeToElement(new { }, OrganicPluginProtocolCodec.JsonOptions);
    }
    private static string GetString(JsonElement root, string name, string fallback) => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;
    private static double GetDouble(JsonElement root, string name, double fallback) => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) && value.TryGetDouble(out var result) ? result : fallback;
    private static int GetInt(JsonElement root, string name, int fallback) => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : fallback;
    private static bool GetBoolean(JsonElement root, string name, bool fallback) => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback;
}

public sealed class OrganicWorkCoordinator(
    IOrganicPermissionStore permissions,
    IOrganicCapabilityCatalog capabilityCatalog,
    IOrganicWorkExecutor executor,
    ILogger<OrganicWorkCoordinator> logger) : IOrganicWorkCoordinator
{
    private readonly ConcurrentDictionary<Guid, OrganicPluginWorkItem> work = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> workflowGates = new(StringComparer.Ordinal);
    public event Action? Changed;

    public IReadOnlyList<OrganicPluginWorkItem> GetWork() => work.Values.OrderByDescending(item => item.CreatedUtc).Take(250).ToList();
    public OrganicPluginWorkItem? Get(Guid id) => work.GetValueOrDefault(id);

    public async Task<OrganicPluginWorkItem> ReceiveAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var advertised = (await capabilityCatalog.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(capability => string.Equals(capability.Key, envelope.CapabilityKey, StringComparison.OrdinalIgnoreCase));
        var permission = permissions.Resolve(envelope.SourcePeerId, envelope.CapabilityKey, envelope.Organs.FirstOrDefault() ?? string.Empty)
            ?? permissions.Resolve(envelope.SourcePeerId, envelope.CapabilityKey);
        var exposed = advertised is not null && advertised.IsEnabled && advertised.IsOnline &&
            advertised.AllowPeerInvocation && permissions.IsCapabilityExposed(envelope.SourcePeerId, advertised) &&
            (permission?.AllowInvocation ?? true);
        if (advertised is not null)
        {
            envelope.RequiresHumanInteractionOnTargetSystem = permission?.RequiresFrontendConfirmation
                ?? advertised.RequiresFrontendUserConfirmation
                || advertised.RequiresHumanConfirmation;
            envelope.Properties ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            envelope.Properties["InteractionEditor"] = JsonSerializer.SerializeToElement(
                (permission?.InteractionEditor ?? advertised.InteractionEditor).ToString(), OrganicPluginProtocolCodec.JsonOptions);
            envelope.Properties["ConfigurationKey"] = JsonSerializer.SerializeToElement(
                advertised.ConfigurationKey, OrganicPluginProtocolCodec.JsonOptions);
        }
        var item = new OrganicPluginWorkItem
        {
            MessageId = envelope.MessageId, CorrelationId = envelope.CorrelationId, PeerId = envelope.SourcePeerId,
            CapabilityKey = envelope.CapabilityKey, Request = envelope,
            Status = !exposed || permissions.IsDenied(envelope)
                ? OrganicWorkStatus.Declined
                : permissions.IsAllowed(envelope)
                    ? OrganicWorkStatus.Queued
                    : OrganicWorkStatus.PendingApproval
        };
        if (item.Status == OrganicWorkStatus.Declined)
            item.Error = advertised is null
                ? "The requested capability is not registered in PublisherStudio."
                : !exposed
                    ? "The PublisherStudio frontend catalog does not expose this capability to the connected peer."
                    : "Denied by the PublisherStudio organic permission policy.";
        work[item.Id] = item;
        Changed?.Invoke();
        if (item.Status == OrganicWorkStatus.Queued)
            await ExecuteAsync(item, cancellationToken).ConfigureAwait(false);
        else if (item.Status == OrganicWorkStatus.PendingApproval)
            logger.LogInformation("Organic work {WorkItemId} awaits PublisherStudio user approval for {CapabilityKey}.", item.Id, item.CapabilityKey);
        else
            logger.LogWarning("Organic work {WorkItemId} was denied by the PublisherStudio permission policy for {CapabilityKey}.", item.Id, item.CapabilityKey);
        return item;
    }

    public async Task<OrganicPluginWorkItem?> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!work.TryGetValue(id, out var item) || item.Status != OrganicWorkStatus.PendingApproval) return item;
        item.Request.UserConfirmed = true;
        item.Status = OrganicWorkStatus.Queued;
        item.UpdatedUtc = DateTimeOffset.UtcNow;
        Changed?.Invoke();
        await ExecuteAsync(item, cancellationToken).ConfigureAwait(false);
        return item;
    }

    public bool UpdateInteractionValue(Guid id, string value)
    {
        if (!work.TryGetValue(id, out var item) || item.Status != OrganicWorkStatus.PendingApproval)
            return false;
        item.Request.InteractionValueJson = value ?? string.Empty;
        var editor = ReadInteractionEditor(item.Request);
        item.Request.InteractionValueContentType = editor == OrganicInteractionEditor.Json
            ? "application/json"
            : "text/plain; charset=utf-8";
        item.UpdatedUtc = DateTimeOffset.UtcNow;
        Changed?.Invoke();
        return true;
    }

    private static OrganicInteractionEditor ReadInteractionEditor(OrganicWireEnvelope envelope)
    {
        if (envelope.Properties is not null && envelope.Properties.TryGetValue("InteractionEditor", out var value))
        {
            if (value.ValueKind == JsonValueKind.String && Enum.TryParse<OrganicInteractionEditor>(value.GetString(), true, out var parsed))
                return parsed;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numeric) && Enum.IsDefined(typeof(OrganicInteractionEditor), numeric))
                return (OrganicInteractionEditor)numeric;
        }
        return envelope.RequiresHumanInteractionOnTargetSystem ? OrganicInteractionEditor.ConfirmationOnly : OrganicInteractionEditor.None;
    }

    public bool Decline(Guid id, string reason)
    {
        if (!work.TryGetValue(id, out var item) || item.Status != OrganicWorkStatus.PendingApproval) return false;
        item.Status = OrganicWorkStatus.Declined;
        item.Error = string.IsNullOrWhiteSpace(reason) ? "Declined by PublisherStudio user." : reason;
        item.UpdatedUtc = DateTimeOffset.UtcNow;
        Changed?.Invoke();
        return true;
    }

    private async Task ExecuteAsync(OrganicPluginWorkItem item, CancellationToken cancellationToken)
    {
        var key = string.IsNullOrWhiteSpace(item.Request.WorkOrderKey) ? item.Id.ToString("N") : item.Request.WorkOrderKey;
        var workflowGate = workflowGates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await workflowGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (item.Request.ExecutionMode == OrganicExecutionMode.Scheduled && item.Request.NotBeforeUtc is { } notBefore && notBefore > DateTimeOffset.UtcNow)
            {
                item.Status = OrganicWorkStatus.Queued;
                item.UpdatedUtc = DateTimeOffset.UtcNow;
                Changed?.Invoke();
                var delay = notBefore - DateTimeOffset.UtcNow;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            item.Status = OrganicWorkStatus.Running;
            item.UpdatedUtc = DateTimeOffset.UtcNow;
            Changed?.Invoke();
            item.ResultJson = await executor.ExecuteAsync(item.Request, cancellationToken).ConfigureAwait(false);
            item.Status = OrganicWorkStatus.Completed;
            item.Error = string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            item.Status = OrganicWorkStatus.Cancelled;
            item.Error = "Cancelled.";
        }
        catch (Exception ex)
        {
            item.Status = OrganicWorkStatus.Failed;
            item.Error = ex.Message;
            logger.LogError(ex, "Organic work {WorkItemId} failed for {CapabilityKey}.", item.Id, item.CapabilityKey);
        }
        finally
        {
            item.UpdatedUtc = DateTimeOffset.UtcNow;
            workflowGate.Release();
            Changed?.Invoke();
        }
    }
}

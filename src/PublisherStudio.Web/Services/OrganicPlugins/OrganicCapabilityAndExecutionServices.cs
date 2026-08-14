using PublisherStudio.BusinessObjects;
using PublisherStudio.Services;
using PublisherStudio.Services.Automation;
using PublisherStudio.Services.Documentation;
using PublisherStudio.Services.MediaConversion;
using PublisherStudio.Services.OpenScad;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Maintains the authoritative directory of organic capability entries used for discovery, validation, and runtime lookup.
/// </summary>
/// <param name="mediaConversion">Media conversion service dependency used by the organic capability workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OrganicCapabilityCatalog(
    IMediaConversionService mediaConversion,
    ILogger<OrganicCapabilityCatalog> logger) : IOrganicCapabilityCatalog
{
    /// <summary>
    /// Occurs when this reviewed legacy metadata catalog changes. Its current descriptors are runtime-derived and do not own external mutation state.
    /// </summary>
    public event Action? Changed
    {
        add { }
        remove { }
    }

    /// <summary>
    /// Retrieves capabilities in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
    try
    {
            var media = await mediaConversion.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            var capabilities = new List<OrganicCapabilityDescriptor>
            {
                Capability("publisher.screen.capture", "Eyes: screen capture", "Captures one user-selected screen or window only after PublisherStudio confirmation and the browser's current getDisplayMedia prompt.", "eyes", false, true, false),
                Capability("publisher.screen.record", "Eyes: screen recording", "Records a user-selected screen or window for up to 15 seconds after PublisherStudio confirmation and a fresh browser prompt.", "eyes", false, true, false),
                Capability("publisher.screen.capture.result", "Eyes: screen capture result", "Reads the bounded status and result of a previously queued browser screenshot for the next council heartbeat.", "eyes", true, false, true),
                Capability("publisher.screenreader.start", "Start recurring screen-reader help", "Starts one debounced single-flight screen-reader session. The interval is clamped to at least 15 seconds.", "eyes", false, true, true),
                Capability("publisher.screenreader.stop", "Stop recurring screen-reader help", "Stops a recurring screen-reader session by id.", "eyes", false, true, true),
                Capability("publisher.input.execute", "Hands: browser input", "Queues one bounded mouse, keyboard, hover, focus or gesture command through the existing automation service.", "hands", false, true, true),
                Capability("publisher.input.result", "Hands: browser input result", "Reads the status and bounded result of a previously queued browser input command.", "eyes", true, false, true),
                Capability("publisher.openscad.generate", "OpenSCAD canonical project generation", "Validates and renders a canonical OpenScadDocument/OpenScadNode graph through the existing registered renderer path.", "hands", false, true, false),
                Capability("publisher.spreadsheet.inspect", "Spreadsheet session inspection", "Returns bounded metadata and preview evidence for an existing workbook session without mutation.", "eyes", true, true, true),
                Capability("publisher.text.insert.propose", "Propose text insertion", "Creates a reviewable text insertion proposal. It never mutates a publication automatically.", "hands", false, true, true),
                Capability("publisher.text.edit.request", "Request reviewed text", "Opens a bounded PublisherStudio text editor after approval, returns the user's saved text through the same CorrelationId, then closes the editor automatically.", "hands", false, true, false),
                Capability("publisher.website.content.request", "Request approved web/document content", "Returns user-approved bounded HTML, DIV or document content plus an optional source URL for LocalGPT chat and other organic clients.", "eyes", true, true, false),
                Capability("publisher.business-context", "PublisherStudio project/API context", "Returns the current domain, service and controller context for grounded council planning.", "eyes", true, false, false),
                Capability("publisher.media.capabilities", "FFmpeg/media capabilities", $"Returns the installed PublisherStudio media conversion capability map. FFmpeg available: {media.Available}.", "eyes", true, false, false)
            };
            logger.LogDebug("Published {CapabilityCount} PublisherStudio organic capabilities.", capabilities.Count);
            return capabilities;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(GetCapabilitiesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(GetCapabilitiesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves skills in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<OrganicSkillDescriptor>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(GetSkillsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(GetSkillsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves UI features in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public Task<IReadOnlyList<OrganicUiFeatureDescriptor>> GetUiFeaturesAsync(CancellationToken cancellationToken = default) {
    try
    {
        return Task.FromResult<IReadOnlyList<OrganicUiFeatureDescriptor>>
        ([
            new() { Key = "publisherstudio.council.run", DisplayName = "AI Council ribbon", State = OrganicUiFeatureState.Enabled, RequiredCapabilityKeys = ["council.run"] },
            new() { Key = "publisherstudio.council.team-picker", DisplayName = "Council team picker", State = OrganicUiFeatureState.Enabled, RequiredCapabilityKeys = ["council.teams"] },
            new() { Key = "publisherstudio.screenreader.recurring", DisplayName = "Recurring screen-reader help", State = OrganicUiFeatureState.Enabled, RequiredCapabilityKeys = ["publisher.screen.capture"] },
            new() { Key = "publisherstudio.spreadsheet.ai", DisplayName = "Spreadsheet AI help", State = OrganicUiFeatureState.Enabled, RequiredCapabilityKeys = ["publisher.spreadsheet.inspect", "council.run"] },
            new() { Key = "publisherstudio.openscad.ai", DisplayName = "OpenSCAD Team", State = OrganicUiFeatureState.Enabled, RequiredCapabilityKeys = ["publisher.openscad.generate", "council.run"] },
            new() { Key = "publisherstudio.picture.ocr", DisplayName = "Picture Studio OCR", State = OrganicUiFeatureState.Enabled, RequiredCapabilityKeys = ["localgpt.vision.ocr"] },
            new() { Key = "publisherstudio.security", DisplayName = "Secure 1-Wire link", State = OrganicUiFeatureState.Enabled, RequiredCapabilityKeys = [] }
        ]);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(GetUiFeaturesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(GetUiFeaturesAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Retrieves hardware in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public Task<IReadOnlyList<OrganicHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default) {
    try
    {
        return Task.FromResult<IReadOnlyList<OrganicHardwareDescriptor>>
        ([
            new()
            {
                Kind = OrganicHardwareKind.Cpu, Index = 0, Name = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "CPU",
                Vendor = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? string.Empty,
                LogicalProcessorCount = Environment.ProcessorCount, IsOnline = true
            }
        ]);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(GetHardwareAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(GetHardwareAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs capability in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the organic capability operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the organic capability operation and used when producing its result.</param>
    /// <param name="description">Description value supplied to the organic capability operation and used when producing its result.</param>
    /// <param name="organ">Organ value supplied to the organic capability operation and used when producing its result.</param>
    /// <param name="readOnly">Value indicating whether read only should apply to this operation.</param>
    /// <param name="confirmation">Value indicating whether confirmation should apply to this operation.</param>
    /// <param name="scheduling">Value indicating whether scheduling should apply to this operation.</param>
    /// <returns>The organic capability descriptor produced by the operation.</returns>
    private OrganicCapabilityDescriptor Capability(string key, string name, string description, string organ, bool readOnly, bool confirmation, bool scheduling) {
    try
    {
        return new()
    {
        Key = key, DisplayName = name, Description = description, Controller = "OrganicPlugins", Method = "POST",
        Route = $"/api/organic/capabilities/{key}", Organs = [organ], Skills = Skills(key), RequiredSkillKeys = Skills(key),
        UiActivationKeys = UiActivationKeys(key), IsOnline = true, IsEnabled = true,
        IsReadOnly = readOnly, RequiresHumanConfirmation = confirmation, SupportsScheduling = scheduling,
        SupportsRecurringExecution = key is "publisher.screenreader.start",
        RequiresHumanInteractionOnTargetSystem = confirmation,
        RequiresAutomatedInteractionOnTargetSystem = key is "publisher.screen.capture" or "publisher.screen.record" or "publisher.input.execute" or "publisher.screenreader.start" or "publisher.screenreader.stop",
        IsExposedToPeer = true,
        AllowPeerInvocation = true,
        RequiresFrontendUserConfirmation = confirmation,
        InteractionEditor = key is "publisher.text.insert.propose" or "publisher.text.edit.request" or "publisher.website.content.request"
            ? OrganicInteractionEditor.RichText
            : confirmation
                ? OrganicInteractionEditor.ConfirmationOnly
                : OrganicInteractionEditor.None,
        ConfigurationKey = $"publisher:{key}",
        ParameterSchemaJson = Schema(key),
        InputContract = InputContract(key),
        OutputContract = OutputContract(key),
        SecurityContract = SecurityContract(key),
        OrganicUseCase = OrganicUseCase(key),
        SuggestedCouncilRoles = SuggestedCouncilRoles(key),
        Source = "PublisherStudio"
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(Capability)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(Capability)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs UI activation keys in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the organic capability operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> UiActivationKeys(string key) {
    try
    {
        return key switch
    {
        "publisher.screen.capture" => ["publisherstudio.screenreader.recurring"],
        "publisher.screenreader.start" => ["publisherstudio.screenreader.recurring"],
        "publisher.screenreader.stop" => ["publisherstudio.screenreader.recurring"],
        "publisher.openscad.generate" => ["publisherstudio.openscad.ai"],
        "publisher.spreadsheet.inspect" => ["publisherstudio.spreadsheet.ai"],
        _ => []
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(UiActivationKeys)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(UiActivationKeys)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs skills in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the organic capability operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> Skills(string key) {
    try
    {
        return key switch
    {
        "publisher.screen.capture" => ["vision", "screen", "screenshot"],
        "publisher.screen.capture.result" => ["vision", "screen", "screenshot", "evidence"],
        "publisher.screen.record" => ["vision", "screen", "video", "evidence"],
        "publisher.screenreader.start" => ["vision", "screen", "screenreader", "recurring"],
        "publisher.screenreader.stop" => ["vision", "screen", "screenreader", "recurring"],
        "publisher.input.execute" => ["mouse", "keyboard", "gesture", "navigation"],
        "publisher.input.result" => ["mouse", "keyboard", "gesture", "verification"],
        "publisher.openscad.generate" => ["openscad", "3d", "geometry", "render"],
        "publisher.spreadsheet.inspect" => ["spreadsheet", "workbook", "analysis"],
        "publisher.text.insert.propose" => ["text", "editing", "proposal"],
        "publisher.text.edit.request" => ["text", "editing", "human-feedback"],
        "publisher.website.content.request" => ["html", "document", "website", "approved-content"],
        "publisher.media.capabilities" => ["ffmpeg", "video", "audio", "conversion"],
        _ => ["publisherstudio", "project-context"]
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(Skills)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(Skills)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs schema in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the organic capability operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Schema(string key) {
    try
    {
        return key switch
    {
        "publisher.screen.capture" => "{\"type\":\"object\",\"properties\":{\"selector\":{\"type\":\"string\"},\"format\":{\"type\":\"string\"},\"quality\":{\"type\":\"number\"}}}",
        "publisher.screen.capture.result" => "{\"type\":\"object\",\"required\":[\"requestId\"],\"properties\":{\"requestId\":{\"type\":\"string\"},\"includeData\":{\"type\":\"boolean\"}}}",
        "publisher.screen.record" => "{\"type\":\"object\",\"properties\":{\"maximumSeconds\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":15},\"includeAudio\":{\"type\":\"boolean\"}}}",
        "publisher.screenreader.start" => "{\"type\":\"object\",\"properties\":{\"selector\":{\"type\":\"string\"},\"prompt\":{\"type\":\"string\"},\"intervalSeconds\":{\"type\":\"integer\",\"minimum\":15}}}",
        "publisher.screenreader.stop" => "{\"type\":\"object\",\"required\":[\"sessionId\"],\"properties\":{\"sessionId\":{\"type\":\"string\"}}}",
        "publisher.input.execute" => "{\"type\":\"object\",\"required\":[\"kind\"],\"properties\":{\"kind\":{\"type\":\"string\"},\"selector\":{\"type\":\"string\"},\"text\":{\"type\":\"string\"},\"key\":{\"type\":\"string\"},\"x\":{\"type\":\"number\"},\"y\":{\"type\":\"number\"}}}",
        "publisher.input.result" => "{\"type\":\"object\",\"required\":[\"requestId\"],\"properties\":{\"requestId\":{\"type\":\"string\"}}}",
        "publisher.openscad.generate" => "{\"type\":\"object\",\"required\":[\"document\"],\"properties\":{\"document\":{\"type\":\"object\"}}}",
        "publisher.spreadsheet.inspect" => "{\"type\":\"object\",\"required\":[\"sessionId\"],\"properties\":{\"sessionId\":{\"type\":\"string\"}}}",
        "publisher.text.insert.propose" => "{\"type\":\"object\",\"required\":[\"text\"],\"properties\":{\"target\":{\"type\":\"string\"},\"text\":{\"type\":\"string\"},\"reason\":{\"type\":\"string\"}}}",
        "publisher.text.edit.request" => "{\"type\":\"object\",\"properties\":{\"title\":{\"type\":\"string\"},\"question\":{\"type\":\"string\"},\"initialText\":{\"type\":\"string\"}}}",
        "publisher.website.content.request" => "{\"type\":\"object\",\"properties\":{\"format\":{\"enum\":[\"html\",\"div\",\"document\"]},\"sourceUrl\":{\"type\":\"string\"},\"maximumCharacters\":{\"type\":\"integer\",\"maximum\":200000}}}",
        _ => "{\"type\":\"object\",\"properties\":{}}"
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(Schema)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(Schema)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs input contract in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the organic capability operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string InputContract(string key) {
    try
    {
        return key switch
    {
        "publisher.screen.capture" => "No image path. The PublisherStudio user selects a screen/window in a fresh browser prompt.",
        "publisher.screen.record" => "maximumSeconds 1..15 and optional audio flag; the user selects the capture source in the browser.",
        "publisher.text.edit.request" => "A title, question and optional initial text. The human edits the value in PublisherStudio.",
        "publisher.website.content.request" => "Requested html/div/document format, optional source URL and maximum character count.",
        _ => $"Parameters matching: {Schema(key)}"
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(InputContract)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(InputContract)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs output contract in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the organic capability operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string OutputContract(string key) {
    try
    {
        return key switch
    {
        "publisher.screen.capture" => "A bounded PNG data URL and pixel metadata returned in the current WorkResult.",
        "publisher.screen.record" => "A bounded WebM data URL, duration and MIME type returned in the current WorkResult.",
        "publisher.text.edit.request" => "The exact user-saved text, ContentType and CorrelationId; the editor closes after Save & return.",
        "publisher.website.content.request" => "Approved bounded content, format, optional source URL and truncation metadata.",
        _ => "A bounded JSON WorkResult associated with the original CorrelationId."
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(OutputContract)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(OutputContract)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs security contract in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the organic capability operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SecurityContract(string key) {
    try
    {
        return key switch
    {
        "publisher.screen.capture" or "publisher.screen.record" => "Always asks in LocalGPT, always asks in PublisherStudio, and always invokes the browser's current getDisplayMedia permission prompt. Saved permission cannot bypass it.",
        "publisher.text.edit.request" or "publisher.website.content.request" => "Requires the receiving PublisherStudio frontend for every current request. Content is encrypted after MFA trust is established.",
        _ => "Requires an explicitly linked peer and the PublisherStudio exposure/invocation permission matrix."
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(SecurityContract)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(SecurityContract)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs organic use case in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the organic capability operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string OrganicUseCase(string key) {
    try
    {
        return key switch
    {
        "publisher.screen.capture" or "publisher.screen.record" => "Eyes organ for current visual evidence.",
        "publisher.text.edit.request" => "Human feedback organ for Council questions and reviewed text.",
        "publisher.website.content.request" => "Approved document/web-content organ usable by LocalGPT and other 1-Wire clients.",
        _ => "PublisherStudio organic capability."
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(OrganicUseCase)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(OrganicUseCase)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs suggested council roles in the organic capability directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the organic capability operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<string> SuggestedCouncilRoles(string key) {
    try
    {
        return key switch
    {
        "publisher.screen.capture" or "publisher.screen.record" => ["vision member", "evidence verifier"],
        "publisher.text.edit.request" => ["council leader", "human collaboration coordinator"],
        "publisher.website.content.request" => ["web/document specialist", "content reviewer"],
        _ => Skills(key)
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(SuggestedCouncilRoles)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicCapabilityCatalog)}.{nameof(SuggestedCouncilRoles)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents an organic work executor application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="codec">Organic plugin protocol codec dependency used by the organic work executor workflow to provide the corresponding application capability.</param>
/// <param name="input">User input automation service dependency used by the organic work executor workflow to provide the corresponding application capability.</param>
/// <param name="screenshots">Screenshot capture service dependency used by the organic work executor workflow to provide the corresponding application capability.</param>
/// <param name="openScad">Open openscad document service dependency used by the organic work executor workflow to provide the corresponding application capability.</param>
/// <param name="spreadsheetSessions">Spreadsheet session store dependency used by the organic work executor workflow to provide the corresponding application capability.</param>
/// <param name="businessContext">Business object context service dependency used by the organic work executor workflow to provide the corresponding application capability.</param>
/// <param name="documentation">Publisher documentation catalog service dependency used by the organic work executor workflow to provide the corresponding application capability.</param>
/// <param name="mediaConversion">Media conversion service dependency used by the organic work executor workflow to provide the corresponding application capability.</param>
/// <param name="resultStore">Organic result store dependency used by the organic work executor workflow to provide the corresponding application capability.</param>
/// <param name="recurringScreenReader">Recurring screen reader service dependency used by the organic work executor workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OrganicWorkExecutor(
    IOrganicPluginProtocolCodec codec,
    IUserInputAutomationService input,
    IScreenshotCaptureService screenshots,
    IOpenScadDocumentService openScad,
    SpreadsheetSessionStore spreadsheetSessions,
    IBusinessObjectContextService businessContext,
    IPublisherDocumentationCatalogService documentation,
    IMediaConversionService mediaConversion,
    IOrganicResultStore resultStore,
    IRecurringScreenReaderService recurringScreenReader,
    ILogger<OrganicWorkExecutor> logger) : IOrganicWorkExecutor
{
    /// <summary>
    /// Performs execute for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public async Task<string> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
    try
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
                    "publisher.screen.capture" => ReadSecureCapture(envelope, "image"),
                    "publisher.screen.record" => ReadSecureCapture(envelope, "video"),
                    "publisher.screen.capture.result" => ReadScreenshotResult(parameters),
                    "publisher.input.execute" => QueueInput(parameters),
                    "publisher.input.result" => ReadInputResult(parameters),
                    "publisher.openscad.generate" => GenerateOpenScad(parameters),
                    "publisher.spreadsheet.inspect" => InspectSpreadsheet(parameters),
                    "publisher.text.insert.propose" => ProposeText(parameters),
                    "publisher.text.edit.request" => ReturnReviewedText(envelope, parameters),
                    "publisher.website.content.request" => ReturnApprovedWebContent(envelope, parameters),
                    "publisher.business-context" => businessContext.CreateSnapshot(),
                    "publisher.documentation.profile" => new { Status = documentation.GetStatus(), HtmlRoute = "/api/documentation/html/index.html", ApiRoute = "/api/documentation/html/api/index.html", PdfRoute = "/api/documentation/pdf", ProfileRoute = "/api/documentation/profile" },
                    "publisher.media.capabilities" => await mediaConversion.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false),
                    _ => throw new KeyNotFoundException($"Unknown organic capability '{envelope.CapabilityKey}'.")
                };
            }
            logger.LogInformation("Executed organic capability {CapabilityKey} for correlation {CorrelationId}.", envelope.CapabilityKey, envelope.CorrelationId);
            return JsonSerializer.Serialize(result, codec.JsonOptions);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ExecuteAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ExecuteAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs queue screenshot for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private object QueueScreenshot(JsonElement parameters)
    {
    try
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
            return new
            {
                queued.Id,
                queued.Status,
                RequiresPublisherFrontendConfirmation = true,
                RequiresBrowserUserGesture = true,
                RequiresCurrentBrowserSessionPermission = true,
                BrowserPermissionCannotBePreGrantedByLocalGpt = true,
                NextCapability = "publisher.screen.capture.result followed by council continuation"
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(QueueScreenshot)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(QueueScreenshot)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads secure capture for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="expectedKind">Expected kind value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private object ReadSecureCapture(OrganicWireEnvelope envelope, string expectedKind)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(envelope.InteractionValueJson))
                throw new InvalidOperationException("The PublisherStudio browser did not return current capture data after approval.");
            using var document = JsonDocument.Parse(envelope.InteractionValueJson);
            var root = document.RootElement;
            var kind = GetString(root, "kind", string.Empty);
            var dataUrl = GetString(root, "dataUrl", string.Empty);
            var mimeType = GetString(root, "mimeType", string.Empty);
            if (!string.Equals(kind, expectedKind, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(dataUrl))
                throw new InvalidDataException($"The browser returned no approved {expectedKind} capture.");
            if (System.Text.Encoding.UTF8.GetByteCount(dataUrl) > OrganicWireProtocol.MaximumMessageBytes - 65536)
                throw new InvalidOperationException("The browser capture exceeds the bounded 1-Wire message size.");
            return new
            {
                Kind = kind, DataUrl = dataUrl, MimeType = mimeType,
                PixelWidth = GetInt(root, "width", 0), PixelHeight = GetInt(root, "height", 0),
                DurationMilliseconds = GetInt(root, "durationMilliseconds", 0),
                CapturedUtc = DateTimeOffset.UtcNow,
                RequiresHumanReview = true
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ReadSecureCapture)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ReadSecureCapture)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs return reviewed text for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="parameters">Parameters value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private object ReturnReviewedText(OrganicWireEnvelope envelope, JsonElement parameters)
    {
    try
    {
            var text = envelope.InteractionValueJson ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("The PublisherStudio user saved no text for the current request.");
            if (text.Length > 200000) text = text[..200000];
            return new
            {
                Text = text,
                Title = GetString(parameters, "title", "LocalGPT text request"),
                ContentType = envelope.InteractionValueContentType,
                envelope.CorrelationId,
                SavedByPublisherStudioUser = true,
                EditorClosedAfterSave = true
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ReturnReviewedText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ReturnReviewedText)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs return approved web content for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="parameters">Parameters value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private object ReturnApprovedWebContent(OrganicWireEnvelope envelope, JsonElement parameters)
    {
    try
    {
            var content = envelope.InteractionValueJson ?? string.Empty;
            var maximum = Math.Clamp(GetInt(parameters, "maximumCharacters", 120000), 1000, 200000);
            var truncated = content.Length > maximum;
            if (truncated) content = content[..maximum];
            return new
            {
                Format = GetString(parameters, "format", "html"),
                SourceUrl = GetString(parameters, "sourceUrl", string.Empty),
                Content = content,
                Truncated = truncated,
                MaximumCharacters = maximum,
                ApprovedByPublisherStudioUser = true,
                envelope.CorrelationId
            };
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ReturnApprovedWebContent)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ReturnApprovedWebContent)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs queue input for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private object QueueInput(JsonElement parameters)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(QueueInput)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(QueueInput)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads screenshot result for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private object ReadScreenshotResult(JsonElement parameters)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ReadScreenshotResult)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ReadScreenshotResult)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads input result for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private object ReadInputResult(JsonElement parameters)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ReadInputResult)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ReadInputResult)} failed.");
        throw;
    }
}

    /// <summary>
    /// Generates open OpenSCAD for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private object GenerateOpenScad(JsonElement parameters)
    {
    try
    {
            if (!parameters.TryGetProperty("document", out var documentElement)) throw new ArgumentException("document is required.");
            var document = documentElement.Deserialize<OpenScadDocument>(codec.JsonOptions) ?? throw new ArgumentException("document is invalid.");
            return openScad.Generate(document);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(GenerateOpenScad)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(GenerateOpenScad)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs inspect spreadsheet for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private object InspectSpreadsheet(JsonElement parameters)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(InspectSpreadsheet)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(InspectSpreadsheet)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs propose text for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="parameters">Parameters value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private object ProposeText(JsonElement parameters)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ProposeText)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ProposeText)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads parameters for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The JSON element produced by the operation.</returns>
    private JsonElement ReadParameters(OrganicWireEnvelope envelope)
    {
    try
    {
            if (envelope.Properties is not null && envelope.Properties.TryGetValue("Parameters", out var parameters)) return parameters;
            return JsonSerializer.SerializeToElement(new { }, codec.JsonOptions);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ReadParameters)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(ReadParameters)} failed.");
        throw;
    }
}
    /// <summary>
    /// Retrieves string for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="root">Root value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string GetString(JsonElement root, string name, string fallback) {
    try
    {
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(GetString)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(GetString)} failed.");
        throw;
    }
}
    /// <summary>
    /// Retrieves double for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="root">Root value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double GetDouble(JsonElement root, string name, double fallback) {
    try
    {
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) && value.TryGetDouble(out var result) ? result : fallback;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(GetDouble)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(GetDouble)} failed.");
        throw;
    }
}
    /// <summary>
    /// Retrieves int for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="root">Root value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the organic work executor operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int GetInt(JsonElement root, string name, int fallback) {
    try
    {
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : fallback;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(GetInt)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(GetInt)} failed.");
        throw;
    }
}
    /// <summary>
    /// Retrieves boolean for <see cref="OrganicWorkExecutor"/>, keeping the operation consistent with the state and invariants of the surrounding organic work executor workflow.
    /// </summary>
    /// <param name="root">Root value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the organic work executor operation and used when producing its result.</param>
    /// <param name="fallback">Value indicating whether fallback should apply to this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetBoolean(JsonElement root, string name, bool fallback) {
    try
    {
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback;
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(GetBoolean)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkExecutor)}.{nameof(GetBoolean)} failed.");
        throw;
    }
}
}

/// <summary>
/// Represents an organic work application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="codec">Organic plugin protocol codec dependency used by the organic work workflow to provide the corresponding application capability.</param>
/// <param name="permissions">Organic permission store dependency used by the organic work workflow to provide the corresponding application capability.</param>
/// <param name="capabilityCatalog">Organic capability catalog dependency used by the organic work workflow to provide the corresponding application capability.</param>
/// <param name="executor">Organic work executor dependency used by the organic work workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OrganicWorkCoordinator(
    IOrganicPluginProtocolCodec codec,
    IOrganicPermissionStore permissions,
    IOrganicCapabilityCatalog capabilityCatalog,
    IOrganicWorkExecutor executor,
    ILogger<OrganicWorkCoordinator> logger) : IOrganicWorkCoordinator
{
    /// <summary>
    /// Stores the in-memory work collection maintained internally by <see cref="OrganicWorkCoordinator"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, OrganicPluginWorkItem> work = new();
    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to workflow gates state owned by <see cref="OrganicWorkCoordinator"/>.
    /// </summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> workflowGates = new(StringComparer.Ordinal);
    /// <summary>
    /// Occurs when changed changes or completes in <see cref="OrganicWorkCoordinator"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Retrieves work for <see cref="OrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<OrganicPluginWorkItem> GetWork() {
    try
    {
        return work.Values.OrderByDescending(item => item.CreatedUtc).Take(250).ToList();
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(GetWork)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(GetWork)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs get for <see cref="OrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The organic plugin work item produced by the operation.</returns>
    public OrganicPluginWorkItem? Get(Guid id) {
    try
    {
        return work.GetValueOrDefault(id);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(Get)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(Get)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs receive for <see cref="OrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic work operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The organic plugin work item produced by the operation.</returns>
    public async Task<OrganicPluginWorkItem> ReceiveAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
    try
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
                    (permission?.InteractionEditor ?? advertised.InteractionEditor).ToString(), codec.JsonOptions);
                envelope.Properties["ConfigurationKey"] = JsonSerializer.SerializeToElement(
                    advertised.ConfigurationKey, codec.JsonOptions);
            }
            var requiresFreshCaptureConsent = envelope.CapabilityKey is "publisher.screen.capture" or "publisher.screen.record";
            if (requiresFreshCaptureConsent)
            {
                // Screen sharing is deliberately non-persistable. Even an AllowAlways rule may not bypass
                // the current PublisherStudio confirmation and the browser's fresh getDisplayMedia prompt.
                envelope.RequiresHumanInteractionOnTargetSystem = true;
                envelope.ApprovalMode = OrganicApprovalMode.AskEveryTime;
                envelope.NormalizeInteractionKind();
            }
            var item = new OrganicPluginWorkItem
            {
                MessageId = envelope.MessageId, CorrelationId = envelope.CorrelationId, PeerId = envelope.SourcePeerId,
                CapabilityKey = envelope.CapabilityKey, Request = envelope,
                Status = !exposed || permissions.IsDenied(envelope)
                    ? OrganicWorkStatus.Declined
                    : requiresFreshCaptureConsent
                        ? OrganicWorkStatus.PendingApproval
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(ReceiveAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(ReceiveAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs approve for <see cref="OrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The organic plugin work item produced by the operation.</returns>
    public async Task<OrganicPluginWorkItem?> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
    try
    {
            if (!work.TryGetValue(id, out var item) || item.Status != OrganicWorkStatus.PendingApproval) return item;
            item.Request.UserConfirmed = true;
            item.Status = OrganicWorkStatus.Queued;
            item.UpdatedUtc = DateTimeOffset.UtcNow;
            Changed?.Invoke();
            await ExecuteAsync(item, cancellationToken).ConfigureAwait(false);
            return item;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(ApproveAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(ApproveAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Updates interaction value for <see cref="OrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="value">Value value supplied to the organic work operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool UpdateInteractionValue(Guid id, string value)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(UpdateInteractionValue)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(UpdateInteractionValue)} failed.");
        throw;
    }
}

    /// <summary>
    /// Reads interaction editor for <see cref="OrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic work operation and used when producing its result.</param>
    /// <returns>The organic interaction editor produced by the operation.</returns>
    private OrganicInteractionEditor ReadInteractionEditor(OrganicWireEnvelope envelope)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(ReadInteractionEditor)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(ReadInteractionEditor)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs decline for <see cref="OrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="reason">Reason value supplied to the organic work operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Decline(Guid id, string reason)
    {
    try
    {
            if (!work.TryGetValue(id, out var item) || item.Status != OrganicWorkStatus.PendingApproval) return false;
            item.Status = OrganicWorkStatus.Declined;
            item.Error = string.IsNullOrWhiteSpace(reason) ? "Declined by PublisherStudio user." : reason;
            item.UpdatedUtc = DateTimeOffset.UtcNow;
            Changed?.Invoke();
            return true;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(Decline)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(OrganicWorkCoordinator)}.{nameof(Decline)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs execute for <see cref="OrganicWorkCoordinator"/>, keeping the operation consistent with the state and invariants of the surrounding organic work workflow.
    /// </summary>
    /// <param name="item">Item value supplied to the organic work operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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

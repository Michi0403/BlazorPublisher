using System.Text.Json;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Describes one browser-facing PublisherStudio AI chat request. The browser never receives LocalGPT transport secrets or provider credentials.
/// </summary>
public sealed class PublisherAiChatRequest
{
    /// <summary>Gets or sets the user-visible prompt submitted by an AI-enabled publication component.</summary>
    /// <value>The prompt value exposed by <see cref="PublisherAiChatRequest"/>.</value>
    public string Prompt { get; set; } = string.Empty;
    /// <summary>Gets or sets the LocalGPT Council team key selected by the publication author.</summary>
    /// <value>The team key value exposed by <see cref="PublisherAiChatRequest"/>.</value>
    public string TeamKey { get; set; } = "general";
    /// <summary>Optional executable organic capability keys required by the user goal.</summary>
    /// <value>Catalog keys to be resolved by LocalGPT before Council dispatch.</value>
    public List<string> RequiredCapabilities { get; set; } = [];
    /// <summary>Gets or sets optional author instructions that are prepended as publication context.</summary>
    /// <value>The system prompt value exposed by <see cref="PublisherAiChatRequest"/>.</value>
    public string SystemPrompt { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether LocalGPT Council memory may be read for this request.</summary>
    /// <value>The include memory value exposed by <see cref="PublisherAiChatRequest"/>.</value>
    public bool IncludeMemory { get; set; } = true;
    /// <summary>Gets or sets a value indicating whether the Council result may be saved to LocalGPT memory.</summary>
    /// <value>The save to memory value exposed by <see cref="PublisherAiChatRequest"/>.</value>
    public bool SaveToMemory { get; set; } = true;
    /// <summary>Gets or sets the maximum visible answer-token budget requested for this component interaction.</summary>
    /// <value>The max output tokens value exposed by <see cref="PublisherAiChatRequest"/>.</value>
    public int MaxOutputTokens { get; set; } = 8192;
    /// <summary>Optional Council round limit selected by the calling publication component.</summary>
    /// <value>Maximum number of Council rounds for this request.</value>
    public int MaxRounds { get; set; } = 1;
    /// <summary>Optional concurrency budget, independently of Council team membership.</summary>
    /// <value>Maximum simultaneous AI participants.</value>
    public int MaxParallelModels { get; set; } = 1;
    /// <summary>Whether the human explicitly requested a Council-generated implementation artifact.</summary>
    /// <value>Whether Council should generate a project artifact.</value>
    public bool GenerateImplementationArtifact { get; set; }
}

/// <summary>
/// Represents the bounded result returned to an AI-enabled publication component after LocalGPT completes the requested Council run.
/// </summary>
public sealed class PublisherAiChatResponse
{
    /// <summary>
    /// Gets or sets the text value that forms part of the publisher AI chat state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="PublisherAiChatResponse"/>.</value>
    public string Text { get; set; } = string.Empty;
    /// <summary>Gets or sets the correlated 1-Wire request identifier used for diagnostics.</summary>
    /// <value>The correlation identifier value exposed by <see cref="PublisherAiChatResponse"/>.</value>
    public Guid CorrelationId { get; set; }
    /// <summary>Gets or sets the LocalGPT Council run identifier when one is present in the result.</summary>
    /// <value>The run identifier value exposed by <see cref="PublisherAiChatResponse"/>.</value>
    public string RunId { get; set; } = string.Empty;
    /// <summary>Stable LocalGPT goal identity, even when a decomposed goal uses several Council runs.</summary>
    /// <value>Goal checkpoint identity; blank for older LocalGPT versions.</value>
    public string GoalId { get; set; } = string.Empty;
    /// <summary>The complete structured Council result for callers that support detailed steps or artifacts.</summary>
    /// <value>JSON returned unchanged from the trusted paired LocalGPT 1-Wire result.</value>
    public string ResultJson { get; set; } = string.Empty;
}

/// <summary>
/// Defines the PublisherStudio bridge from browser-facing AI components to the already paired LocalGPT 1-Wire connection.
/// </summary>
public interface IPublisherAiBridgeService
{
    /// <summary>
    /// Determines whether available as part of the publisher AI bridge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool IsAvailable();
    /// <summary>Runs one Council-backed chat interaction and returns only its final human-visible answer to the publication component.</summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The publisher AI chat response produced by the operation.</returns>
    Task<PublisherAiChatResponse> ChatAsync(PublisherAiChatRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Keeps AI-enabled publication components provider-neutral by adapting their requests to PublisherStudio's secured LocalGPT 1-Wire connection.
/// </summary>
/// <param name="localGpt">Local gpt connection service dependency used by the publisher AI bridge workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublisherAiBridgeService(
    ILocalGptConnectionService localGpt,
    ILogger<PublisherAiBridgeService> logger) : IPublisherAiBridgeService
{
    /// <summary>
    /// Determines whether available as part of the publisher AI bridge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public bool IsAvailable()
    {
        try
        {
            return localGpt.State.IsLinked && localGpt.State.HasCapability("council.run");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio could not evaluate the LocalGPT AI bridge state.");
            throw;
        }
    }

    /// <summary>
    /// Performs chat as part of the publisher AI bridge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public async Task<PublisherAiChatResponse> ChatAsync(PublisherAiChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!IsAvailable())
                throw new InvalidOperationException("LocalGPT is not linked or does not currently advertise Council execution.");
            var prompt = (request.Prompt ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("A chat message is required.", nameof(request));

            var teamKey = string.IsNullOrWhiteSpace(request.TeamKey) ? "general" : request.TeamKey.Trim();
            var systemPrompt = (request.SystemPrompt ?? string.Empty).Trim();
            var councilRequest = new OrganicCouncilPromptRequest
            {
                TeamKey = teamKey,
                RequestedOrganicCapabilities = request.RequiredCapabilities
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                MaxRounds = Math.Clamp(request.MaxRounds, 1, 128),
                MaxParallelModels = Math.Clamp(request.MaxParallelModels, 1, 32),
                MaxOutputTokens = Math.Clamp(request.MaxOutputTokens, 256, 262144),
                MaxContextTokens = 262144,
                IncludeMemory = request.IncludeMemory,
                // Publisher-started Council work is intentionally a normal LocalGPT /chat session.
                // Keep it durable so provider reasoning, function calls/results and the final answer
                // remain available after the correlated 1-Wire request itself has completed.
                SaveToMemory = request.SaveToMemory,
                GenerateImplementationArtifact = request.GenerateImplementationArtifact,
                // A browser request must never self-attest human approval. Consequential
                // artifacts use the separately maintained Council / human-collaboration gate.
                UserConfirmedArtifactBuild = false,
                ExternalProjectContextJson = JsonSerializer.Serialize(new
                {
                    Source = "PublisherStudio DevExtreme Chat",
                    ComponentRuntime = "standalone-compatible",
                    TeamKey = teamKey
                })
            };
            // Keep the author's goal intact. The former answer-only wrapper suppressed tool-oriented work.
            // Author instructions originate from the request, not an internal hardcoded Council prompt.
            councilRequest.Prompt = string.IsNullOrWhiteSpace(systemPrompt)
                ? prompt
                : string.Concat(systemPrompt, "\n\n", prompt);

            var correlationId = await localGpt.SendCouncilRequestAsync(councilRequest, cancellationToken).ConfigureAwait(false);
            var deadline = DateTimeOffset.UtcNow.AddMinutes(10);
            while (true)
            {
                var remaining = deadline - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    throw new TimeoutException("The LocalGPT Council did not complete within ten minutes.");
                var response = await localGpt.WaitForResultAsync(correlationId, remaining, cancellationToken).ConfigureAwait(false);
                if (response.MessageType == OrganicWireMessageType.ApprovalRequired)
                {
                    logger.LogInformation("PublisherStudio AI chat request {CorrelationId} is waiting for LocalGPT approval.", correlationId);
                    continue;
                }
                if (response.MessageType == OrganicWireMessageType.Error)
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Error) ? "LocalGPT returned an AI execution error." : response.Error);
                if (response.MessageType != OrganicWireMessageType.WorkResult)
                    continue;

                var resultJson = ReadPropertyString(response, "ResultJson");
                if (string.IsNullOrWhiteSpace(resultJson))
                    throw new InvalidDataException("LocalGPT returned an empty Council result.");
                var (text, runId, goalId) = ReadCouncilResult(resultJson);
                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidDataException("LocalGPT completed the Council run without a visible final answer.");
                logger.LogInformation("PublisherStudio AI chat request {CorrelationId} completed through LocalGPT Council run {RunId}.", correlationId, runId);
                return new PublisherAiChatResponse { Text = text, CorrelationId = correlationId, RunId = runId, GoalId = goalId, ResultJson = resultJson };
            }
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(exception, "PublisherStudio AI chat request was canceled by its caller.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio AI chat request failed: {Message}", exception.Message);
            throw;
        }
    }

    /// <summary>
    /// Reads property string as part of the publisher AI bridge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the publisher AI bridge operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the publisher AI bridge operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ReadPropertyString(OrganicWireEnvelope envelope, string name)
    {
        try
        {
            if (envelope.Properties is null || !envelope.Properties.TryGetValue(name, out var element)) return string.Empty;
            return element.ValueKind == JsonValueKind.String ? element.GetString() ?? string.Empty : element.GetRawText();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio could not read LocalGPT result property {PropertyName}.", name);
            throw;
        }
    }

    /// <summary>
    /// Reads council result as part of the publisher AI bridge service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="json">Json value supplied to the publisher AI bridge operation and used when producing its result.</param>
    /// <returns>The string text string run identifier produced by the operation.</returns>
    private (string Text, string RunId, string GoalId) ReadCouncilResult(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return (string.Empty, string.Empty, string.Empty);
        string text = string.Empty;
        string runId = string.Empty;
        string goalId = string.Empty;
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals("FinalAnswer", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                text = property.Value.GetString() ?? string.Empty;
            else if (property.Name.Equals("RunId", StringComparison.OrdinalIgnoreCase))
                runId = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : property.Value.GetRawText().Trim('"');
            else if (property.Name.Equals("GoalExecution", StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var detail in property.Value.EnumerateObject())
                {
                    if (detail.Name.Equals("GoalId", StringComparison.OrdinalIgnoreCase) &&
                        detail.Value.ValueKind == JsonValueKind.String)
                        goalId = detail.Value.GetString() ?? string.Empty;
                }
            }
        }
        if (string.IsNullOrWhiteSpace(text) && root.TryGetProperty("Steps", out var steps) &&
            steps.ValueKind == JsonValueKind.Array)
        {
            foreach (var step in steps.EnumerateArray().Reverse())
            {
                if (step.ValueKind != JsonValueKind.Object) continue;
                if (step.TryGetProperty("VisibleContent", out var visible) && visible.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(visible.GetString()))
                {
                    text = visible.GetString() ?? string.Empty;
                    break;
                }
                if (step.TryGetProperty("Content", out var content) && content.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(content.GetString()))
                {
                    text = content.GetString() ?? string.Empty;
                    break;
                }
            }
            if (!string.IsNullOrWhiteSpace(text))
                logger.LogWarning("Council {RunId} returned no final answer; using recorded visible step output.", runId);
        }
        return (text, runId, goalId);
    }
}

using System.Collections.Frozen;
using System.Text.RegularExpressions;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Coordinates publisher runtime pattern behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed class PublisherRuntimePatternService : IPublisherRuntimePatternService
{
    /// <summary>
    /// Stores the in-memory patterns collection maintained internally by <see cref="PublisherRuntimePatternService"/> for its current workflow state.
    /// </summary>
    private readonly FrozenDictionary<PublisherRuntimePattern, Regex> patterns;
    /// <summary>
    /// Stores the logger used by <see cref="PublisherRuntimePatternService"/> to record operational diagnostics without coupling callers to logging details.
    /// </summary>
    private readonly ILogger<PublisherRuntimePatternService> logger;

    /// <summary>
    /// Initializes a new <see cref="PublisherRuntimePatternService"/> instance and captures the dependencies or initial state required by its publisher runtime pattern workflow.
    /// </summary>
    /// <param name="options">Options containing the caller-supplied values that control this operation.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public PublisherRuntimePatternService(
        PublisherRuntimePolicyOptions options,
        ILogger<PublisherRuntimePatternService> logger)
    {
        try
        {
            this.logger = logger;
            patterns = options.RegexPatterns.ToFrozenDictionary(
                item => item.Key,
                item => Compile(item.Key, item.Value));
            logger.LogInformation($"Compiled {patterns.Count} PublisherStudio runtime regex policies.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not compile the PublisherStudio runtime regex policies.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves regex as part of the publisher runtime pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pattern">Pattern value supplied to the publisher runtime pattern operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    public Regex GetRegex(PublisherRuntimePattern pattern)
    {
        try
        {
            if (!patterns.TryGetValue(pattern, out var regex))
                throw new KeyNotFoundException($"The runtime regex policy '{pattern}' is not configured.");

            logger.LogTrace($"Resolved PublisherStudio runtime regex policy '{pattern}'.");
            return regex;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not resolve PublisherStudio runtime regex policy '{pattern}'.");
            throw;
        }
    }

    /// <summary>
    /// Performs compile as part of the publisher runtime pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the publisher runtime pattern operation and used when producing its result.</param>
    /// <param name="policy">Policy value supplied to the publisher runtime pattern operation and used when producing its result.</param>
    /// <returns>The regex produced by the operation.</returns>
    private Regex Compile(PublisherRuntimePattern key, PublisherRegexPolicy policy)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(policy.Pattern))
                throw new InvalidDataException($"Runtime regex policy '{key}' has no pattern.");
            if (policy.TimeoutMilliseconds <= 0)
                throw new InvalidDataException($"Runtime regex policy '{key}' has an invalid timeout.");

            var options = ParseOptions(policy.Options);
            return new Regex(policy.Pattern, options, TimeSpan.FromMilliseconds(policy.TimeoutMilliseconds));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not compile PublisherStudio runtime regex policy '{key}'.");
            throw;
        }
    }

    /// <summary>
    /// Parses options as part of the publisher runtime pattern service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publisher runtime pattern operation and used when producing its result.</param>
    /// <returns>The regex options produced by the operation.</returns>
    private RegexOptions ParseOptions(string value)
    {
        try
        {
            var result = RegexOptions.None;
            foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Enum.TryParse<RegexOptions>(token, ignoreCase: true, out var parsed))
                    throw new InvalidDataException($"Unknown RegexOptions value '{token}'.");
                result |= parsed;
            }
            logger.LogTrace($"Parsed PublisherStudio regex options '{value}'.");
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not parse PublisherStudio regex options '{value}'.");
            throw;
        }
    }
}

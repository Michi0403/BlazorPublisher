using System.Collections.Frozen;
using System.Text.RegularExpressions;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Provides publisher runtime pattern service operations.
/// </summary>
public sealed class PublisherRuntimePatternService : IPublisherRuntimePatternService
{
    private readonly FrozenDictionary<PublisherRuntimePattern, Regex> patterns;
    private readonly ILogger<PublisherRuntimePatternService> logger;

    /// <summary>
    /// Publishes er runtime pattern service.
    /// </summary>
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
    /// Gets regex.
    /// </summary>
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
    /// Runs the compile operation.
    /// </summary>
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
    /// Parses options.
    /// </summary>
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

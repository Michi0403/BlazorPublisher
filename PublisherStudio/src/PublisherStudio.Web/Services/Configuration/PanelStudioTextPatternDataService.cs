using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Serializable-object-store boundary for panel text patterns. The reviewed seed travels
/// with the application; an optional LocalApplicationData override can replace entries
/// without moving runtime values back into components, controllers, or business services.
/// </summary>
public sealed class PanelStudioTextPatternDataService : IPanelStudioTextPatternDataService
{
    private readonly IReadOnlyDictionary<string, Regex> _patterns;
    private readonly ILogger<PanelStudioTextPatternDataService> logger;
    public PanelStudioTextPatternDataService(
        IWebHostEnvironment environment,
        IOptions<PanelTextPatternStoreOptions> options,
        ILogger<PanelStudioTextPatternDataService> logger)
    {
        try
        {
            this.logger = logger;
            var settings = options.Value;
            ValidateOptions(settings);
            var seedPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, settings.SeedPath));
            var definitions = ReadStore(seedPath);

            var overridePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PublisherStudio",
                settings.OverrideDirectoryName,
                settings.OverrideFileName);
            if (File.Exists(overridePath))
            {
                foreach (var pair in ReadStore(overridePath))
                    definitions[pair.Key] = pair.Value;
            }

            _patterns = definitions.ToDictionary(
                pair => pair.Key,
                pair => Compile(pair.Key, pair.Value),
                StringComparer.Ordinal);
            RequirePattern(nameof(ShutdownPattern));
            RequirePattern(nameof(HtmlBreakPattern));
            RequirePattern(nameof(HtmlTagPattern));
            RequirePattern(nameof(UnsafeFileNamePattern));
            logger.LogInformation(
                "Loaded {PatternCount} PublisherStudio panel text patterns from serializable object storage; pattern content omitted.",
                _patterns.Count);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio panel text pattern object-store initialization failed.");
            throw;
        }
    }

    public Regex ShutdownPattern => RequirePattern(nameof(ShutdownPattern));
    public Regex HtmlBreakPattern => RequirePattern(nameof(HtmlBreakPattern));
    public Regex HtmlTagPattern => RequirePattern(nameof(HtmlTagPattern));
    public Regex UnsafeFileNamePattern => RequirePattern(nameof(UnsafeFileNamePattern));

    private Regex RequirePattern(string name)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (_patterns.TryGetValue(name, out var pattern))
            {
                return pattern;
            }

            throw new KeyNotFoundException($"Required panel text pattern '{name}' is missing from the serializable object store.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                $"Required panel text pattern {{PatternName}} could not be resolved from serializable object storage; pattern content omitted.",
                name);
            throw;
        }
    }

    private Dictionary<string, PatternDefinition> ReadStore(string path)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            if (!File.Exists(path))
                throw new FileNotFoundException("Panel text pattern store was not found.", path);

            var document = JsonSerializer.Deserialize<PatternStoreDocument>(
                File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidDataException("Panel text pattern store is empty or invalid.");
            if (document.SchemaVersion <= 0 || document.Patterns.Count == 0)
                throw new InvalidDataException("Panel text pattern store does not contain a valid schema and pattern set.");

            return new Dictionary<string, PatternDefinition>(document.Patterns, StringComparer.Ordinal);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                $"Panel text pattern object store {{StoreFileName}} could not be read; path and pattern content omitted.",
                Path.GetFileName(path));
            throw;
        }
    }

    private Regex Compile(string name, PatternDefinition definition)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(definition);
            if (string.IsNullOrWhiteSpace(definition.Pattern))
                throw new InvalidDataException($"Panel text pattern '{name}' has no pattern text.");
            if (definition.TimeoutMilliseconds <= 0)
                throw new InvalidDataException($"Panel text pattern '{name}' has an invalid timeout.");

            var options = RegexOptions.None;
            foreach (var option in definition.Options)
            {
                if (!Enum.TryParse<RegexOptions>(option, true, out var parsed))
                    throw new InvalidDataException($"Panel text pattern '{name}' has unknown option '{option}'.");
                options |= parsed;
            }

            var compiled = new Regex(
                definition.Pattern,
                options,
                TimeSpan.FromMilliseconds(definition.TimeoutMilliseconds));
            return compiled;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                $"Panel text pattern {{PatternName}} could not be compiled from serializable object storage; pattern content omitted.",
                name);
            throw;
        }
    }

    private void ValidateOptions(PanelTextPatternStoreOptions options)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.SeedPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.OverrideDirectoryName);
            ArgumentException.ThrowIfNullOrWhiteSpace(options.OverrideFileName);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                $"PublisherStudio panel text pattern object-store options failed validation; configured values omitted.");
            throw;
        }
    }

}

internal sealed class PatternStoreDocument
{
    public PatternStoreDocument() { }
    public int SchemaVersion { get; set; }
    public Dictionary<string, PatternDefinition> Patterns { get; set; } = new(StringComparer.Ordinal);
}

internal sealed class PatternDefinition
{
    public PatternDefinition() { }
    public string Pattern { get; set; } = string.Empty;
    public List<string> Options { get; set; } = [];
    public int TimeoutMilliseconds { get; set; }
}

using System.Text.Json;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Describes one viewport that Panel Studio can use to simulate a target display size without changing authored panel geometry.
/// </summary>
public sealed class PanelStudioPreviewPreset
{
    /// <summary>Gets or sets the stable key used to select this preview preset.</summary>
    /// <value>The key value exposed by <see cref="PanelStudioPreviewPreset"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name value that forms part of the panel studio preview preset state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PanelStudioPreviewPreset"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the width value that forms part of the panel studio preview preset state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="PanelStudioPreviewPreset"/>.</value>
    public int Width { get; set; }
    /// <summary>
    /// Gets or sets the height value that forms part of the panel studio preview preset state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="PanelStudioPreviewPreset"/>.</value>
    public int Height { get; set; }
    /// <summary>Gets or sets a value indicating whether this preset is shipped by PublisherStudio.</summary>
    /// <value>The built in value exposed by <see cref="PanelStudioPreviewPreset"/>.</value>
    public bool BuiltIn { get; set; }
}

/// <summary>
/// Owns Panel Studio preview viewport presets and persists user presets through the established system-variable store.
/// </summary>
public interface IPanelStudioPreviewPresetService
{
    /// <summary>Gets the built-in and user-defined preview presets in display order.</summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<PanelStudioPreviewPreset> GetPresets();
    /// <summary>Gets one preview preset by key, or <see langword="null"/> when it is unknown.</summary>
    /// <param name="key">Key value supplied to the panel studio preview preset operation and used when producing its result.</param>
    /// <returns>The panel studio preview preset produced by the operation.</returns>
    PanelStudioPreviewPreset? GetPreset(string key);
    /// <summary>
    /// Performs save as part of the panel studio preview preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the panel studio preview preset operation and used when producing its result.</param>
    /// <param name="name">Name value supplied to the panel studio preview preset operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the panel studio preview preset operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the panel studio preview preset operation and used when producing its result.</param>
    /// <returns>The panel studio preview preset produced by the operation.</returns>
    PanelStudioPreviewPreset Save(string? key, string name, int width, int height);
    /// <summary>
    /// Performs delete as part of the panel studio preview preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the panel studio preview preset operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    bool Delete(string key);
}

/// <summary>
/// Persists custom Panel Studio viewport presets while keeping PublisherStudio's built-in device presets immutable.
/// </summary>
/// <param name="variables">System variable store service dependency used by the panel studio preview preset workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PanelStudioPreviewPresetService(
    ISystemVariableStoreService variables,
    ILogger<PanelStudioPreviewPresetService> logger) : IPanelStudioPreviewPresetService
{
    /// <summary>
    /// Stores the internal built ins state used by <see cref="PanelStudioPreviewPresetService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly PanelStudioPreviewPreset[] _builtIns =
    [
        new() { Key = "phone", Name = "Phone", Width = 390, Height = 844, BuiltIn = true },
        new() { Key = "tablet", Name = "Tablet", Width = 768, Height = 1024, BuiltIn = true },
        new() { Key = "laptop", Name = "Laptop", Width = 1280, Height = 720, BuiltIn = true },
        new() { Key = "wide", Name = "Wide", Width = 1440, Height = 900, BuiltIn = true }
    ];

    /// <summary>
    /// Retrieves presets as part of the panel studio preview preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public IReadOnlyList<PanelStudioPreviewPreset> GetPresets()
    {
        try
        {
            var user = ReadUserPresets();
            return _builtIns.Select(Clone).Concat(user.Select(Clone)).ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel Studio preview presets could not be loaded.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves preset as part of the panel studio preview preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public PanelStudioPreviewPreset? GetPreset(string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            return GetPresets().FirstOrDefault(item => string.Equals(item.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel Studio preview preset {PresetKey} could not be resolved.", key);
            throw;
        }
    }

    /// <summary>
    /// Performs save as part of the panel studio preview preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public PanelStudioPreviewPreset Save(string? key, string name, int width, int height)
    {
        try
        {
            var cleanName = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(cleanName)) throw new ArgumentException("A preview preset name is required.", nameof(name));
            if (width is < 160 or > 16384) throw new ArgumentOutOfRangeException(nameof(width), "Preview width must be between 160 and 16384 pixels.");
            if (height is < 120 or > 16384) throw new ArgumentOutOfRangeException(nameof(height), "Preview height must be between 120 and 16384 pixels.");

            var cleanKey = NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(cleanKey))
                cleanKey = $"custom-{Guid.NewGuid():N}";
            if (_builtIns.Any(item => string.Equals(item.Key, cleanKey, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Built-in preview presets cannot be overwritten.");

            var user = ReadUserPresets();
            var existing = user.FirstOrDefault(item => string.Equals(item.Key, cleanKey, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                existing = new PanelStudioPreviewPreset { Key = cleanKey, BuiltIn = false };
                user.Add(existing);
            }
            existing.Name = cleanName;
            existing.Width = width;
            existing.Height = height;
            Persist(user);
            logger.LogInformation("Saved Panel Studio preview preset {PresetKey} ({Width}x{Height}).", existing.Key, width, height);
            return Clone(existing);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel Studio preview preset could not be saved.");
            throw;
        }
    }

    /// <summary>
    /// Performs delete as part of the panel studio preview preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <inheritdoc />
    public bool Delete(string key)
    {
        try
        {
            var cleanKey = NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(cleanKey)) return false;
            if (_builtIns.Any(item => string.Equals(item.Key, cleanKey, StringComparison.OrdinalIgnoreCase)))
                return false;
            var user = ReadUserPresets();
            var removed = user.RemoveAll(item => string.Equals(item.Key, cleanKey, StringComparison.OrdinalIgnoreCase)) > 0;
            if (removed)
            {
                Persist(user);
                logger.LogInformation("Deleted Panel Studio preview preset {PresetKey}.", cleanKey);
            }
            return removed;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel Studio preview preset {PresetKey} could not be deleted.", key);
            throw;
        }
    }

    /// <summary>
    /// Reads user presets as part of the panel studio preview preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<PanelStudioPreviewPreset> ReadUserPresets()
    {
        var json = variables.GetString(variables.PanelStudioPreviewPresetsVariableName, "[]");
        try
        {
            var items = JsonSerializer.Deserialize<List<PanelStudioPreviewPreset>>(json) ?? [];
            return items
                .Where(item => !string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Name))
                .Where(item => item.Width is >= 160 and <= 16384 && item.Height is >= 120 and <= 16384)
                .Where(item => _builtIns.All(builtIn => !string.Equals(builtIn.Key, item.Key, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last())
                .Select(item => { item.BuiltIn = false; return item; })
                .ToList();
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Stored Panel Studio preview presets were invalid JSON; built-in presets remain available.");
            return [];
        }
    }

    /// <summary>
    /// Performs persist as part of the panel studio preview preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="presets">Presets value supplied to the panel studio preview preset operation and used when producing its result.</param>
    private void Persist(List<PanelStudioPreviewPreset> presets)
    {
        try
        {
            variables.Set(variables.PanelStudioPreviewPresetsVariableName, JsonSerializer.Serialize(presets));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel Studio preview presets could not be persisted.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes key as part of the panel studio preview preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="key">Key value supplied to the panel studio preview preset operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeKey(string? key)
    {
        try
        {
            return (key ?? string.Empty).Trim().ToLowerInvariant();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel Studio preview preset key could not be normalized.");
            throw;
        }
    }

    /// <summary>
    /// Performs clone as part of the panel studio preview preset service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="source">Source value supplied to the panel studio preview preset operation and used when producing its result.</param>
    /// <returns>The panel studio preview preset produced by the operation.</returns>
    private PanelStudioPreviewPreset Clone(PanelStudioPreviewPreset source)
    {
        try
        {
            return new PanelStudioPreviewPreset
            {
                Key = source.Key,
                Name = source.Name,
                Width = source.Width,
                Height = source.Height,
                BuiltIn = source.BuiltIn
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel Studio preview preset {PresetKey} could not be cloned.", source?.Key);
            throw;
        }
    }
}

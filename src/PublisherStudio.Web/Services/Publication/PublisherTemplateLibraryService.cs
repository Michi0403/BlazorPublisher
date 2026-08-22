using System.Text.Json;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Panels;

namespace PublisherStudio.Services.Publication;

/// <summary>
/// Defines the local PublisherStudio template-library capability used by editor surfaces without coupling them to filesystem details.
/// </summary>
public interface IPublisherTemplateLibraryService
{
    /// <summary>Returns the canonical user-editable LocalApplicationData folder scanned for complete publication template JSON files.</summary>
    /// <value>The canonical LocalApplicationData PublisherTemplates directory.</value>
    string PublisherTemplateDirectory { get; }
    /// <summary>Returns the canonical user-editable LocalApplicationData folder scanned for reusable Panel/Div template JSON files.</summary>
    /// <value>The canonical LocalApplicationData DivTemplates directory.</value>
    string DivTemplateDirectory { get; }
    /// <summary>Ensures both local template directories and the non-destructive shipped starter templates exist.</summary>
    void EnsureTemplateDirectories();
    /// <summary>Discovers valid full-publication templates currently present in the local template directory.</summary>
    /// <returns>The valid publication templates ordered for display.</returns>
    IReadOnlyList<PublicationTemplateDescriptor> GetPublicationTemplates();
    /// <summary>Discovers valid reusable Panel/Div templates currently present in the local template directory.</summary>
    /// <returns>The valid Div templates ordered for display.</returns>
    IReadOnlyList<DivTemplateDescriptor> GetDivTemplates();
    /// <summary>Creates an isolated new-publication JSON payload from a local template.</summary>
    /// <param name="templateId">File-relative template identifier returned by <see cref="GetPublicationTemplates"/>.</param>
    /// <returns>A normalized publication JSON payload with a fresh publication identifier.</returns>
    string CreatePublicationJson(string templateId);
    /// <summary>Creates a detached Panel/Div instance from a local reusable template.</summary>
    /// <param name="templateId">File-relative template identifier returned by <see cref="GetDivTemplates"/>.</param>
    /// <param name="document">Destination publication whose object identifiers must remain unique.</param>
    /// <returns>A normalized detached panel ready to insert into the destination document.</returns>
    PanelElement CreateDivTemplate(string templateId, PublicationDocument document);
}

/// <summary>
/// Loads user-editable publication and Panel/Div templates from the canonical PublisherStudio LocalApplicationData root.
/// Shipped starter templates are copied only when the corresponding local file is missing, so user edits are never overwritten.
/// </summary>
/// <param name="environment">Host environment used to locate the shipped seed templates.</param>
/// <param name="files">Publication file service used for the same polymorphic serialization and normalization as ordinary documents.</param>
/// <param name="panels">Panel document service used to normalize reusable Div templates against their destination publication.</param>
/// <param name="logger">Logger used to record template discovery and recoverable invalid-file diagnostics.</param>
public sealed class PublisherTemplateLibraryService(
    IWebHostEnvironment environment,
    PublicationFileService files,
    PanelDocumentService panels,
    ILogger<PublisherTemplateLibraryService> logger) : IPublisherTemplateLibraryService
{
    /// <summary>Names the LocalApplicationData folder that owns complete publication templates.</summary>
    private const string PublisherDirectoryName = "PublisherTemplates";
    /// <summary>Names the LocalApplicationData folder that owns reusable Panel/Div templates.</summary>
    private const string DivDirectoryName = "DivTemplates";
    /// <summary>Stores the published application directory containing non-destructive publication starter seeds.</summary>
    private readonly string _publisherSeedDirectory = Path.Combine(environment.ContentRootPath, "Configuration", "Templates", "Publisher");
    /// <summary>Stores the published application directory containing non-destructive Panel/Div starter seeds.</summary>
    private readonly string _divSeedDirectory = Path.Combine(environment.ContentRootPath, "Configuration", "Templates", "Div");

    /// <summary>Gets the LocalApplicationData PublisherTemplates folder used for editable complete-publication templates.</summary>
    /// <value>The canonical per-user publication-template directory.</value>
    public string PublisherTemplateDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PublisherStudio",
        PublisherDirectoryName);

    /// <summary>Gets the LocalApplicationData DivTemplates folder used for editable reusable Panel/Div templates.</summary>
    /// <value>The canonical per-user Div-template directory.</value>
    public string DivTemplateDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PublisherStudio",
        DivDirectoryName);

    /// <summary>Creates both per-user template folders and copies only starter seed files that do not already exist.</summary>
    public void EnsureTemplateDirectories()
    {
        try
        {
            Directory.CreateDirectory(PublisherTemplateDirectory);
            Directory.CreateDirectory(DivTemplateDirectory);
            CopyMissingSeeds(_publisherSeedDirectory, PublisherTemplateDirectory);
            CopyMissingSeeds(_divSeedDirectory, DivTemplateDirectory);
            logger.LogInformation(
                "PublisherStudio template libraries are ready with {PublisherCount} publication templates and {DivCount} Div templates.",
                CountTemplateFiles(PublisherTemplateDirectory),
                CountTemplateFiles(DivTemplateDirectory));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio template-library initialization failed.");
            throw;
        }
    }

    /// <summary>Discovers valid complete publication templates while isolating malformed individual files from the rest of the library.</summary>
    /// <returns>Display descriptors ordered by category and name.</returns>
    public IReadOnlyList<PublicationTemplateDescriptor> GetPublicationTemplates()
    {
        try
        {
            EnsureTemplateDirectories();
            var templates = new List<PublicationTemplateDescriptor>();
            foreach (var path in EnumerateTemplateFiles(PublisherTemplateDirectory))
            {
                try
                {
                    var document = files.Deserialize(File.ReadAllText(path));
                    var animatedObjects = document.Pages.SelectMany(page => page.Elements).Count(element => element.Animations.Count > 0);
                    var pageWord = document.Pages.Count == 1 ? "page" : "pages";
                    var animationText = animatedObjects > 0 ? $" · {animatedObjects} animated objects" : string.Empty;
                    templates.Add(new PublicationTemplateDescriptor(
                        Path.GetFileName(path),
                        string.IsNullOrWhiteSpace(document.Name) ? Path.GetFileNameWithoutExtension(path) : document.Name,
                        $"{document.Pages.Count} {pageWord}{animationText}",
                        "Publisher templates",
                        document.Pages.Count,
                        Path.GetFileName(path),
                        File.GetLastWriteTimeUtc(path)));
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Ignoring invalid publication template file {TemplateFileName}.", Path.GetFileName(path));
                }
            }

            return templates
                .OrderBy(template => template.Category, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(template => template.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio publication-template discovery failed.");
            throw;
        }
    }

    /// <summary>Discovers valid reusable Panel/Div templates while isolating malformed individual files from the rest of the library.</summary>
    /// <returns>Display descriptors ordered by category and name.</returns>
    public IReadOnlyList<DivTemplateDescriptor> GetDivTemplates()
    {
        try
        {
            EnsureTemplateDirectories();
            var templates = new List<DivTemplateDescriptor>();
            foreach (var path in EnumerateTemplateFiles(DivTemplateDirectory))
            {
                try
                {
                    var source = ReadDivTemplate(path);
                    templates.Add(new DivTemplateDescriptor(
                        Path.GetFileName(path),
                        source.Name,
                        source.Description,
                        source.Category,
                        source.IconCssClass,
                        "panel",
                        Path.GetFileName(path),
                        File.GetLastWriteTimeUtc(path)));
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Ignoring invalid Div template file {TemplateFileName}.", Path.GetFileName(path));
                }
            }

            return templates
                .OrderBy(template => template.Category, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(template => template.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PublisherStudio Div-template discovery failed.");
            throw;
        }
    }

    /// <summary>Creates a fresh unsaved publication payload from one validated local publication template file.</summary>
    /// <param name="templateId">File-only identifier returned by publication-template discovery.</param>
    /// <returns>A normalized serialized publication with a fresh document identity.</returns>
    public string CreatePublicationJson(string templateId)
    {
        try
        {
            EnsureTemplateDirectories();
            var path = ResolveTemplatePath(PublisherTemplateDirectory, templateId);
            var document = files.Deserialize(File.ReadAllText(path));
            document.Id = Guid.NewGuid();
            document.ModifiedUtc = DateTimeOffset.UtcNow;
            document.Streaming = new PublicationStreamingSettings();
            return files.Serialize(document);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating a publication from local template {TemplateId} failed.", SafeTemplateName(templateId));
            throw;
        }
    }

    /// <summary>Creates a detached Panel/Div clone with regenerated internal identities for safe repeated insertion.</summary>
    /// <param name="templateId">File-only identifier returned by Div-template discovery.</param>
    /// <param name="document">Destination publication used to avoid identity collisions.</param>
    /// <returns>A normalized detached panel instance ready for insertion.</returns>
    public PanelElement CreateDivTemplate(string templateId, PublicationDocument document)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(document);
            EnsureTemplateDirectories();
            var path = ResolveTemplatePath(DivTemplateDirectory, templateId);
            var source = ReadDivTemplate(path);
            var panel = (PanelElement)files.CloneElement(source.Prototype);
            RegenerateDetachedPanelIdentity(document, panel);
            panels.Normalize(document, panel);
            return panel;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating a Panel/Div from local template {TemplateId} failed.", SafeTemplateName(templateId));
            throw;
        }
    }


    /// <summary>
    /// Regenerates every identity owned by a detached Panel/Div template and rewrites its internal references before insertion.
    /// This prevents repeated inserts from reusing template GUIDs while preserving connector, behavior and component relationships inside the clone.
    /// </summary>
    /// <param name="document">Destination publication whose existing element identifiers must not be reused.</param>
    /// <param name="panel">Detached panel clone to rewrite.</param>
    private void RegenerateDetachedPanelIdentity(PublicationDocument document, PanelElement panel)
    {
        try
        {
            logger.LogTrace("Regenerating detached Panel/Div template identities before insertion.");
            var existingElementIds = document.Pages
                .SelectMany(page => page.Elements)
                .SelectMany(FlattenElements)
                .Select(element => element.Id)
                .Where(id => id != Guid.Empty)
                .ToHashSet();
            foreach (var template in document.ComponentTemplates)
            {
                foreach (var element in FlattenElements(template.Prototype))
                    if (element.Id != Guid.Empty) existingElementIds.Add(element.Id);
            }

            var elements = FlattenElements(panel).ToArray();
            var elementIds = new Dictionary<Guid, Guid>();
            foreach (var element in elements)
            {
                var oldId = element.Id;
                var newId = NextUniqueGuid(existingElementIds);
                if (oldId != Guid.Empty && !elementIds.ContainsKey(oldId)) elementIds.Add(oldId, newId);
                element.Id = newId;
            }

            var groupIds = elements
                .Where(element => element.GroupId is not null)
                .Select(element => element.GroupId!.Value)
                .Distinct()
                .ToDictionary(id => id, _ => Guid.NewGuid());
            var sharedComponentIds = elements
                .OfType<DevExtremeComponentElement>()
                .Where(component => component.SharedComponentId is not null)
                .Select(component => component.SharedComponentId!.Value)
                .Distinct()
                .ToDictionary(id => id, _ => Guid.NewGuid());

            var portIds = new Dictionary<Guid, Guid>();
            foreach (var element in elements)
            {
                foreach (var port in element.ConnectorPorts)
                {
                    var oldPortId = port.Id;
                    port.Id = Guid.NewGuid();
                    if (oldPortId != Guid.Empty && !portIds.ContainsKey(oldPortId)) portIds.Add(oldPortId, port.Id);
                }
            }

            foreach (var element in elements)
            {
                if (element.GroupId is { } groupId && groupIds.TryGetValue(groupId, out var mappedGroupId))
                    element.GroupId = mappedGroupId;

                foreach (var animation in element.Animations) animation.Id = Guid.NewGuid();
                foreach (var behavior in element.Behaviors)
                {
                    behavior.Id = Guid.NewGuid();
                    behavior.TargetElementId = MapLocalElementReference(behavior.TargetElementId, elementIds);
                    behavior.TargetPageId = null;
                }

                if (element.Interaction is not null)
                {
                    element.Interaction.TargetElementId = MapLocalElementReference(element.Interaction.TargetElementId, elementIds);
                    element.Interaction.TargetPageId = null;
                }

                if (element is PublicationMediaElement media)
                    foreach (var segment in media.Segments) segment.Id = Guid.NewGuid();

                if (element is DevExtremeComponentElement component)
                {
                    if (component.SharedComponentId is { } sharedId && sharedComponentIds.TryGetValue(sharedId, out var mappedSharedId))
                        component.SharedComponentId = mappedSharedId;
                    foreach (var field in component.Fields) field.Id = Guid.NewGuid();
                    foreach (var action in component.Actions)
                    {
                        action.Id = Guid.NewGuid();
                        action.TargetElementId = MapLocalElementReference(action.TargetElementId, elementIds);
                        action.TargetPageId = null;
                        action.TargetSharedComponentId = action.TargetSharedComponentId is { } actionSharedId
                            && sharedComponentIds.TryGetValue(actionSharedId, out var mappedActionSharedId)
                                ? mappedActionSharedId
                                : null;
                    }
                    foreach (var componentPanel in component.Panels)
                    {
                        componentPanel.Id = Guid.NewGuid();
                        foreach (var field in componentPanel.Fields) field.Id = Guid.NewGuid();
                    }
                    foreach (var item in component.MenuItems) item.Id = Guid.NewGuid();
                    foreach (var feature in component.VectorFeatures) feature.Id = Guid.NewGuid();
                }

                if (element is ConnectorElement connector)
                {
                    RemapEndpoint(connector.Source, elementIds, portIds);
                    RemapEndpoint(connector.Target, elementIds, portIds);
                    connector.Signal.MotionTargetElementId = MapLocalElementReference(connector.Signal.MotionTargetElementId, elementIds);
                    connector.Signal.CompletionTargetElementId = MapLocalElementReference(connector.Signal.CompletionTargetElementId, elementIds);
                    connector.Signal.NextConnectorId = MapLocalElementReference(connector.Signal.NextConnectorId, elementIds);
                }
            }

            foreach (var nestedPanel in elements.OfType<PanelElement>())
            {
                var viewIds = new Dictionary<Guid, Guid>();
                foreach (var view in nestedPanel.Views)
                {
                    var oldViewId = view.Id;
                    view.Id = Guid.NewGuid();
                    if (oldViewId != Guid.Empty && !viewIds.ContainsKey(oldViewId)) viewIds.Add(oldViewId, view.Id);
                }
                nestedPanel.ActiveViewId = viewIds.TryGetValue(nestedPanel.ActiveViewId, out var activeViewId)
                    ? activeViewId
                    : nestedPanel.Views.FirstOrDefault()?.Id ?? Guid.Empty;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Regenerating detached Panel/Div template identities failed.");
            throw;
        }
    }

    /// <summary>Collects one publication element and every descendant contained by nested panels.</summary>
    /// <param name="element">Root publication element.</param>
    /// <returns>The root and its nested publication elements.</returns>
    private IReadOnlyList<PublicationElement> FlattenElements(PublicationElement element)
    {
        try
        {
            logger.LogTrace("Collecting publication elements from a detached template tree.");
            var result = new List<PublicationElement> { element };
            if (element is not PanelElement panel) return result;
            foreach (var view in panel.Views)
            foreach (var child in view.Elements)
                result.AddRange(FlattenElements(child));
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Collecting publication elements from a detached template tree failed.");
            throw;
        }
    }

    /// <summary>Creates a new GUID that does not collide with the destination publication's current element identities.</summary>
    /// <param name="usedIds">Identifiers already reserved by the destination or this clone.</param>
    /// <returns>A newly reserved GUID.</returns>
    private Guid NextUniqueGuid(HashSet<Guid> usedIds)
    {
        try
        {
            logger.LogTrace("Allocating a collision-free template element identity.");
            Guid value;
            do value = Guid.NewGuid(); while (!usedIds.Add(value));
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Allocating a collision-free template element identity failed.");
            throw;
        }
    }

    /// <summary>Maps a template-local element reference to its freshly generated identity and clears external stale references.</summary>
    /// <param name="candidate">Original template element reference.</param>
    /// <param name="elementIds">Old-to-new element identity map.</param>
    /// <returns>The mapped local identity, or <see langword="null"/> when the reference was not owned by the template.</returns>
    private Guid? MapLocalElementReference(Guid? candidate, IReadOnlyDictionary<Guid, Guid> elementIds)
    {
        try
        {
            logger.LogTrace("Mapping a detached template-local element reference.");
            return candidate is { } id && elementIds.TryGetValue(id, out var mapped) ? mapped : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Mapping a detached template-local element reference failed.");
            throw;
        }
    }

    /// <summary>Rewrites a connector endpoint to a freshly generated local element/port identity or safely detaches an external stale endpoint to canvas.</summary>
    /// <param name="endpoint">Connector endpoint to rewrite.</param>
    /// <param name="elementIds">Old-to-new element identity map.</param>
    /// <param name="portIds">Old-to-new connector-port identity map.</param>
    private void RemapEndpoint(ConnectorEndpoint endpoint, IReadOnlyDictionary<Guid, Guid> elementIds, IReadOnlyDictionary<Guid, Guid> portIds)
    {
        try
        {
            logger.LogTrace("Remapping a detached template connector endpoint.");
            if (endpoint.Kind == ConnectorEndpointKind.Element)
            {
                if (elementIds.TryGetValue(endpoint.ElementId, out var mappedElementId)) endpoint.ElementId = mappedElementId;
                else
                {
                    endpoint.Kind = ConnectorEndpointKind.Canvas;
                    endpoint.ElementId = Guid.Empty;
                    endpoint.PortId = null;
                    return;
                }
            }
            if (endpoint.PortId is { } portId)
                endpoint.PortId = portIds.TryGetValue(portId, out var mappedPortId) ? mappedPortId : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Remapping a detached template connector endpoint failed.");
            throw;
        }
    }

    /// <summary>Reads either the maintained wrapper format or a raw PanelElement JSON file.</summary>
    /// <param name="path">Validated local template path.</param>
    /// <returns>The normalized template metadata and panel prototype.</returns>
    private DivTemplateSource ReadDivTemplate(string path)
    {
        try
        {
            logger.LogTrace("Reading local Div template file {TemplateFileName}.", Path.GetFileName(path));
            var json = File.ReadAllText(path);
            using var parsed = JsonDocument.Parse(json);
            var root = parsed.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("A Div template must contain a JSON object.");

            string name;
            string description;
            string category;
            string iconCssClass;
            string elementJson;
            if (TryGetProperty(root, "prototype", out var prototype))
            {
                elementJson = prototype.GetRawText();
                name = ReadString(root, "name", Path.GetFileNameWithoutExtension(path));
                description = ReadString(root, "description", "Reusable Panel/Div template.");
                category = ReadString(root, "category", "Div templates");
                iconCssClass = ReadString(root, "iconCssClass", "pub-icon pub-icon-panel");
            }
            else
            {
                elementJson = json;
                name = ReadString(root, "name", Path.GetFileNameWithoutExtension(path));
                description = "Reusable Panel/Div template.";
                category = "Div templates";
                iconCssClass = "pub-icon pub-icon-panel";
            }

            var element = files.DeserializeElement(elementJson);
            if (element is not PanelElement panel)
                throw new InvalidDataException("A Div template must contain a PanelElement prototype.");
            if (string.IsNullOrWhiteSpace(name)) name = panel.Name;
            if (string.IsNullOrWhiteSpace(description)) description = $"{Math.Max(1, panel.Views.Count)} reusable panel views.";
            return new DivTemplateSource(
                name.Trim(),
                description.Trim(),
                string.IsNullOrWhiteSpace(category) ? "Div templates" : category.Trim(),
                string.IsNullOrWhiteSpace(iconCssClass) ? "pub-icon pub-icon-panel" : iconCssClass.Trim(),
                panel);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading local Div template file {TemplateFileName} failed.", Path.GetFileName(path));
            throw;
        }
    }

    /// <summary>Copies shipped starter templates into LocalApplicationData without replacing an existing user file.</summary>
    /// <param name="seedDirectory">Published application seed directory.</param>
    /// <param name="targetDirectory">User-editable LocalApplicationData template directory.</param>
    private void CopyMissingSeeds(string seedDirectory, string targetDirectory)
    {
        try
        {
            logger.LogTrace("Copying missing PublisherStudio template seeds from {SeedDirectoryName}.", Path.GetFileName(seedDirectory));
            if (!Directory.Exists(seedDirectory))
            {
                logger.LogWarning("PublisherStudio template seed directory {SeedDirectoryName} is missing.", Path.GetFileName(seedDirectory));
                return;
            }

            foreach (var seedPath in EnumerateTemplateFiles(seedDirectory))
            {
                var targetPath = Path.Combine(targetDirectory, Path.GetFileName(seedPath));
                if (File.Exists(targetPath)) continue;
                File.Copy(seedPath, targetPath, overwrite: false);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Copying missing PublisherStudio template seeds failed.");
            throw;
        }
    }

    /// <summary>Lists supported JSON template files from one directory without descending into arbitrary user paths.</summary>
    /// <param name="directory">Template directory to enumerate.</param>
    /// <returns>The supported local template file paths.</returns>
    private IReadOnlyList<string> EnumerateTemplateFiles(string directory)
    {
        try
        {
            logger.LogTrace("Enumerating PublisherStudio JSON template files.");
            return Directory.Exists(directory)
                ? Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                : [];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Enumerating PublisherStudio JSON template files failed.");
            throw;
        }
    }

    /// <summary>Counts supported JSON template files without parsing their content.</summary>
    /// <param name="directory">Template directory to inspect.</param>
    /// <returns>The number of JSON files in the directory.</returns>
    private int CountTemplateFiles(string directory)
    {
        try
        {
            logger.LogTrace("Counting PublisherStudio JSON template files.");
            return Directory.Exists(directory)
                ? Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly).Count()
                : 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Counting PublisherStudio JSON template files failed.");
            throw;
        }
    }

    /// <summary>Resolves a descriptor identifier to a file inside its owning template directory and rejects traversal or alternate paths.</summary>
    /// <param name="directory">Owning template directory.</param>
    /// <param name="templateId">File-relative identifier returned by discovery.</param>
    /// <returns>The validated template file path.</returns>
    private string ResolveTemplatePath(string directory, string templateId)
    {
        try
        {
            logger.LogTrace("Resolving local PublisherStudio template identifier {TemplateFileName}.", string.IsNullOrWhiteSpace(templateId) ? "(empty)" : Path.GetFileName(templateId));
            ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
            var fileName = Path.GetFileName(templateId);
            if (!string.Equals(fileName, templateId, StringComparison.Ordinal) || !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The template identifier is not a valid local JSON template file name.");
            var path = Path.Combine(directory, fileName);
            if (!File.Exists(path)) throw new FileNotFoundException("The selected PublisherStudio template no longer exists.", fileName);
            return path;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving a local PublisherStudio template identifier failed.");
            throw;
        }
    }

    /// <summary>Reads a case-insensitive JSON string property while preserving a caller-supplied fallback.</summary>
    /// <param name="root">Template JSON root object.</param>
    /// <param name="name">Property name to read.</param>
    /// <param name="fallback">Fallback used when the property is absent or blank.</param>
    /// <returns>The stored string or fallback.</returns>
    private string ReadString(JsonElement root, string name, string fallback)
    {
        try
        {
            logger.LogTrace("Reading template metadata string property {PropertyName}.", name);
            return TryGetProperty(root, name, out var property) && property.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.GetString())
                ? property.GetString()!
                : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading template metadata string property {PropertyName} failed.", name);
            throw;
        }
    }

    /// <summary>Finds a JSON property by name without requiring users to match serializer casing exactly.</summary>
    /// <param name="root">Template JSON object.</param>
    /// <param name="name">Property name to locate.</param>
    /// <param name="value">Located property value when found.</param>
    /// <returns><see langword="true"/> when the property exists.</returns>
    private bool TryGetProperty(JsonElement root, string name, out JsonElement value)
    {
        try
        {
            logger.LogTrace("Resolving template metadata property {PropertyName}.", name);
            foreach (var property in root.EnumerateObject())
            {
                if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                value = property.Value;
                return true;
            }
            value = default;
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Resolving template metadata property {PropertyName} failed.", name);
            throw;
        }
    }

    /// <summary>Reduces an untrusted identifier to its file-name portion before writing it to diagnostics.</summary>
    /// <param name="templateId">Untrusted template identifier.</param>
    /// <returns>A file-name-only diagnostic value.</returns>
    private string SafeTemplateName(string templateId)
    {
        try
        {
            logger.LogTrace("Reducing a template identifier for diagnostics.");
            return string.IsNullOrWhiteSpace(templateId) ? "(empty)" : Path.GetFileName(templateId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reducing a template identifier for diagnostics failed.");
            throw;
        }
    }

    /// <summary>Internal normalized representation of a reusable Div template file.</summary>
    /// <param name="Name">Template display name.</param>
    /// <param name="Description">Template description.</param>
    /// <param name="Category">Palette category.</param>
    /// <param name="IconCssClass">Palette icon class.</param>
    /// <param name="Prototype">Panel prototype to clone for insertion.</param>
    private sealed record DivTemplateSource(
        string Name,
        string Description,
        string Category,
        string IconCssClass,
        PanelElement Prototype);
}

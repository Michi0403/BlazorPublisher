using PublisherStudio.BusinessObjects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PublisherStudio.Services;

/// <summary>
/// Owns publication object addresses, behavior target choices, common method catalogs and
/// browser-runtime behavior serialization so Razor components do not duplicate text or routing logic.
/// </summary>
/// <param name="logger">Logger used to record behavior service diagnostics.</param>
public sealed class PublicationBehaviorService(ILogger<PublicationBehaviorService> logger)
{
    /// <summary>
    /// Stores the shared read-only options value used by <see cref="PublicationBehaviorService"/> across instances of the containing type.
    /// </summary>
    private readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Builds the stable root address for one publication.</summary>
    /// <param name="document">Document value supplied to the publication behavior operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Address(PublicationDocument document)
    {
        try
        {
            return $"publication://{document.Id}";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to build a publication object root address.");
            throw;
        }
    }

    /// <summary>Builds the stable address for a publication page.</summary>
    /// <param name="document">Document value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="page">Page value supplied to the publication behavior operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Address(PublicationDocument document, PublicationPage page)
    {
        try
        {
            return $"{Address(document)}/page/{page.Id}";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to build a publication page object address.");
            throw;
        }
    }

    /// <summary>Builds a stable address for an element independent of its current visual location.</summary>
    /// <param name="document">Document value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="element">Element value supplied to the publication behavior operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Address(PublicationDocument document, PublicationElement element)
    {
        try
        {
            return $"{Address(document)}/element/{element.Id}";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to build a publication element object address.");
            throw;
        }
    }

    /// <summary>Builds a stable human-readable object address for an element inside a panel view.</summary>
    /// <param name="document">Document value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="panel">Panel value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="view">View value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="element">Element value supplied to the publication behavior operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Address(PublicationDocument document, PanelElement panel, PublicationPanelView view, PublicationElement element)
    {
        try
        {
            _ = panel;
            _ = view;
            return Address(document, element);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to build a panel publication object address.");
            throw;
        }
    }

    /// <summary>Returns behavior targets from the edited panel plus other addressable publication objects.</summary>
    /// <param name="document">Document value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="panel">Panel value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="view">View value supplied to the publication behavior operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<PublicationObjectAddressOption> Targets(PublicationDocument document, PanelElement panel, PublicationPanelView view)
    {
        try
        {
            var values = new List<PublicationObjectAddressOption>();
            var seen = new HashSet<Guid>();

            foreach (var item in view.Elements.OrderBy(item => item.ZIndex).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!seen.Add(item.Id)) continue;
                values.Add(new PublicationObjectAddressOption(item.Id, item.Name, item.Kind.ToString(), Address(document, panel, view, item), "Current panel"));
                AddNestedTargets(document, item, "Current panel", values, seen);
            }

            foreach (var page in document.Pages)
            {
                foreach (var item in page.Elements.OrderBy(item => item.ZIndex).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (seen.Add(item.Id))
                    {
                        values.Add(new PublicationObjectAddressOption(item.Id, item.Name, item.Kind.ToString(), Address(document, item), $"Page · {page.Name}"));
                    }
                    AddNestedTargets(document, item, $"Page · {page.Name}", values, seen);
                }
            }

            return values;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to enumerate publication behavior targets.");
            throw;
        }
    }

    /// <summary>Finds an addressable publication element from the edited panel or publication.</summary>
    /// <param name="document">Document value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="panel">Panel value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="elementId">Identifier of the element to use for this operation.</param>
    /// <returns>The publication element produced by the operation.</returns>
    public PublicationElement? FindElement(PublicationDocument document, PanelElement panel, Guid elementId)
    {
        try
        {
            foreach (var view in panel.Views)
            {
                var panelMatch = FindElement(view.Elements, elementId);
                if (panelMatch is not null) return panelMatch;
            }

            foreach (var page in document.Pages)
            {
                var pageMatch = FindElement(page.Elements, elementId);
                if (pageMatch is not null) return pageMatch;
            }

            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to resolve publication element {ElementId}.", elementId);
            throw;
        }
    }

    /// <summary>Returns the allow-listed common runtime methods appropriate for an authored element.</summary>
    /// <param name="element">Element value supplied to the publication behavior operation and used when producing its result.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<string> CommonMethods(PublicationElement element)
    {
        try
        {
            var methods = new List<string> { "click", "focus", "blur", "change", "show", "hide", "toggleVisibility" };
            if (element is DevExtremeComponentElement component)
            {
                methods.AddRange(["repaint", "reset", "enable", "disable", "setValue"]);
                switch (component.ComponentKind)
                {
                    case PublicationComponentKind.DataGrid:
                    case PublicationComponentKind.TreeList:
                        methods.AddRange(["refresh", "clearFilter", "clearSelection", "selectAll"]);
                        break;
                    case PublicationComponentKind.Scheduler:
                        methods.AddRange(["refresh", "scrollToTime"]);
                        break;
                    case PublicationComponentKind.Menu:
                    case PublicationComponentKind.ContextMenu:
                        methods.AddRange(["show", "hide"]);
                        break;
                    default:
                        methods.Add("refresh");
                        break;
                }
            }
            else if (element is DataVisualElement or LiveSourceElement)
            {
                methods.Add("refresh");
            }
            else if (element is PublicationMediaElement)
            {
                methods.AddRange(["play", "pause", "togglePlayback", "mute", "unmute", "setVolume", "seek"]);
            }
            return methods.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to enumerate common publication object methods.");
            throw;
        }
    }

    /// <summary>Returns common methods for a selected behavior target, falling back to the source object.</summary>
    /// <param name="document">Document value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="panel">Panel value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="targetElementId">Identifier of the target element to use for this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<string> CommonMethods(PublicationDocument document, PanelElement panel, PublicationElement source, Guid? targetElementId)
    {
        try
        {
            var target = targetElementId is { } id ? FindElement(document, panel, id) : source;
            return CommonMethods(target ?? source);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to enumerate common methods for a selected publication behavior target.");
            throw;
        }
    }

    /// <summary>Returns ready-to-insert JavaScript examples for an explicitly enabled component custom-script action.</summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<PublicationScriptHelperOption> ScriptHelpers()
    {
        try
        {
            return
            [
                new("current-refresh", "Refresh current component", "Refreshes the component that raised the event.", "await context.objects.current(context.host)?.call('refresh');"),
                new("current-focus", "Focus current component", "Moves focus to the component that raised the event.", "await context.objects.current(context.host)?.call('focus');"),
                new("next-page", "Next publication page", "Moves the publication runtime to the next page.", "context.publication.nextPage();"),
                new("previous-page", "Previous publication page", "Moves the publication runtime to the previous page.", "context.publication.previousPage();"),
                new("log-event", "Inspect event data", "Writes the event payload to browser developer tools without changing the publication.", "console.log('PublisherStudio event', context.data, context.event);")
            ];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to build publication script helper options.");
            throw;
        }
    }

    /// <summary>Returns ready-to-insert JavaScript examples for an isolated HTML embed sandbox.</summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<PublicationScriptHelperOption> HtmlScriptHelpers()
    {
        try
        {
            return
            [
                new("html-click", "Button click", "Connects an element with id action to an element with id result.", "document.querySelector('#action')?.addEventListener('click', () => { document.querySelector('#result').textContent = 'Done'; });"),
                new("html-toggle", "Toggle element", "Toggles an element with id target when an element with id action is clicked.", "document.querySelector('#action')?.addEventListener('click', () => { document.querySelector('#target')?.toggleAttribute('hidden'); });"),
                new("html-clock", "Live clock", "Updates an element with id clock once per second.", "const clock = document.querySelector('#clock'); if (clock) { const updateClock = () => clock.textContent = new Date().toLocaleTimeString(); updateClock(); setInterval(updateClock, 1000); }"),
                new("html-form", "Read form value", "Copies the value of an input with id source into an element with id result.", "document.querySelector('#source')?.addEventListener('input', event => { const result = document.querySelector('#result'); if (result) result.textContent = event.target.value; });")
            ];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to build HTML embed script helper options.");
            throw;
        }
    }

    /// <summary>Finds one script helper by key for either component scripts or isolated HTML embeds.</summary>
    /// <param name="key">Key value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="htmlEmbed">Value indicating whether HTML embed should apply to this operation.</param>
    /// <returns>The publication script helper option produced by the operation.</returns>
    public PublicationScriptHelperOption? FindScriptHelper(string key, bool htmlEmbed)
    {
        try
        {
            var values = htmlEmbed ? HtmlScriptHelpers() : ScriptHelpers();
            return values.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to resolve publication script helper {HelperKey}.", key);
            throw;
        }
    }

    /// <summary>Builds JavaScript that calls one object-interface method by stable publication address.</summary>
    /// <param name="address">Address value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="method">Method value supplied to the publication behavior operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string ScriptCall(string address, string method)
    {
        try
        {
            var addressJson = JsonSerializer.Serialize(address ?? string.Empty);
            var methodJson = JsonSerializer.Serialize(method ?? string.Empty);
            return $"await context.objects.get({addressJson})?.call({methodJson});";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to build a publication object script helper call.");
            throw;
        }
    }

    /// <summary>Appends a helper snippet to an existing custom-script body with readable spacing.</summary>
    /// <param name="existing">Existing value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="snippet">Snippet value supplied to the publication behavior operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string AppendScript(string existing, string snippet)
    {
        try
        {
            var current = existing?.TrimEnd() ?? string.Empty;
            var addition = snippet?.Trim() ?? string.Empty;
            if (current.Length == 0) return addition;
            if (addition.Length == 0) return current;
            return $"{current}{Environment.NewLine}{addition}";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to append a publication script helper snippet.");
            throw;
        }
    }

    /// <summary>Serializes enabled behavior rules for the browser publication runtime.</summary>
    /// <param name="element">Element value supplied to the publication behavior operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Serialize(PublicationElement element)
    {
        try
        {
            return JsonSerializer.Serialize(element.Behaviors.Where(item => item.Enabled), options);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to serialize publication behaviors for element {ElementId}.", element.Id);
            throw;
        }
    }

    /// <summary>Adds a behavior rule to an element and returns the persisted rule.</summary>
    /// <param name="element">Element value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="trigger">Trigger value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="action">Action value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="targetElementId">Identifier of the target element to use for this operation.</param>
    /// <param name="targetPageId">Identifier of the target page to use for this operation.</param>
    /// <param name="method">Method value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="url">Url value supplied to the publication behavior operation and used when producing its result.</param>
    /// <returns>The publication behavior produced by the operation.</returns>
    public PublicationBehavior Add(
        PublicationElement element,
        PublicationBehaviorTrigger trigger,
        PublicationBehaviorAction action,
        Guid? targetElementId = null,
        Guid? targetPageId = null,
        string? method = null,
        string? value = null,
        string? url = null)
    {
        try
        {
            var rule = new PublicationBehavior
            {
                Trigger = trigger,
                Action = action,
                TargetElementId = targetElementId,
                TargetPageId = targetPageId,
                Method = method?.Trim() ?? string.Empty,
                Value = value ?? string.Empty,
                Url = url?.Trim() ?? string.Empty
            };
            element.Behaviors.Add(rule);
            return rule;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to add a publication behavior to element {ElementId}.", element.Id);
            throw;
        }
    }

    /// <summary>
    /// Adds nested targets as part of the publication behavior service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="element">Element value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="scope">Scope value supplied to the publication behavior operation and used when producing its result.</param>
    /// <param name="values">Publication object address option dependency used by the publication behavior workflow to provide the corresponding application capability.</param>
    /// <param name="seen">Guid dependency used by the publication behavior workflow to provide the corresponding application capability.</param>
    private void AddNestedTargets(
        PublicationDocument document,
        PublicationElement element,
        string scope,
        ICollection<PublicationObjectAddressOption> values,
        ISet<Guid> seen)
    {
        try
        {
            if (element is not PanelElement nestedPanel) return;
            foreach (var nestedView in nestedPanel.Views)
            {
                foreach (var child in nestedView.Elements.OrderBy(item => item.ZIndex).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (seen.Add(child.Id))
                    {
                        values.Add(new PublicationObjectAddressOption(child.Id, child.Name, child.Kind.ToString(), Address(document, child), $"{scope} · {nestedPanel.Name}/{nestedView.Name}"));
                    }
                    AddNestedTargets(document, child, $"{scope} · {nestedPanel.Name}/{nestedView.Name}", values, seen);
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to enumerate nested publication object addresses.");
            throw;
        }
    }

    /// <summary>Finds an element by identifier within a materialized publication element tree.</summary>
    /// <param name="elements">Publication element dependency used by the publication behavior workflow to provide the corresponding application capability.</param>
    /// <param name="elementId">Identifier of the element to use for this operation.</param>
    /// <returns>The publication element produced by the operation.</returns>
    private PublicationElement? FindElement(IReadOnlyList<PublicationElement> elements, Guid elementId)
    {
        try
        {
            foreach (var element in elements)
            {
                if (element.Id == elementId) return element;
                if (element is not PanelElement panel) continue;
                foreach (var view in panel.Views)
                {
                    var nested = FindElement(view.Elements, elementId);
                    if (nested is not null) return nested;
                }
            }
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to search a publication element tree for {ElementId}.", elementId);
            throw;
        }
    }
}

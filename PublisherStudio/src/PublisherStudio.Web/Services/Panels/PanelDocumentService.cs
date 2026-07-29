using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using PublisherStudio.Domain;

namespace PublisherStudio.Services.Panels;

public sealed class PanelDocumentService(
    PublicationDataService data,
    PublicationComponentService components,
    ILogger<PanelDocumentService>? logger = null)
{
    private readonly PublicationDataService _data = data;
    private readonly PublicationComponentService _components = components;
    private readonly ILogger<PanelDocumentService> logger = logger ?? NullLogger<PanelDocumentService>.Instance;

    private readonly PublicationPanelPresetDescriptor[] Presets =
    [
        new("blank", "Blank panel", "An empty reusable view with local navigation ready for nested PublisherStudio components.", "Base", false, "pub-icon pub-icon-panel"),
        new("kpi-dashboard", "Live KPI dashboard", "Four live data views sharing the publication data model: KPI, chart, pie and table.", "Dashboard", true, "pub-icon pub-icon-chart"),
        new("operations-board", "Operations board", "A tabbed operations experience with status, details and an embedded web workspace.", "Dashboard", true, "pub-icon pub-icon-data"),
        new("creator-hub", "Creator / gamer hub", "A multi-view creator panel for live sources, media, chat and stream information.", "Creator", true, "pub-icon pub-icon-video"),
        new("web-experience", "Web experience wrapper", "A menu-driven container for isolated interactive HTML experiences and exported web fragments.", "Web", false, "pub-icon pub-icon-web")
    ];

    public IReadOnlyList<PublicationPanelPresetDescriptor> GetPresets()
    {
        try
        {
            logger.LogDebug("Returning {PresetCount} Panel Studio presets.", Presets.Length);
            return Presets;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel Studio preset discovery failed.");
            throw;
        }
    }

    private readonly PanelComponentToolDescriptor[] ComponentTools =
    [
        new("text", "Text", "Rich text frame shared with the Mainframe.", "Content", "pub-icon pub-icon-text", "text"),
        new("picture", "Picture", "Picture frame that can later open in Picture Studio.", "Content", "pub-icon pub-icon-picture", "picture"),
        new("video", "Video", "Video frame compatible with Video Studio and HTML export.", "Content", "pub-icon pub-icon-video", "video"),
        new("audio", "Audio", "Audio player compatible with Audio Studio and HTML export.", "Content", "pub-icon pub-icon-audio", "audio"),
        new("shape", "Shape", "Reusable visual container or accent shape.", "Content", "pub-icon pub-icon-shape", "shape"),
        new("kpi", "KPI", "Live KPI bound to a publication data object.", "Data", "pub-icon pub-icon-chart", "kpi"),
        new("chart", "Chart", "Live chart bound to publication or web data.", "Data", "pub-icon pub-icon-chart", "chart"),
        new("table", "Data table", "Read-only data table using the shared data model.", "Data", "pub-icon pub-icon-data", "table"),
        new("datagrid", "Data Grid", "Interactive DevExtreme grid with reusable connection settings.", "Interactive", "pub-icon pub-icon-data", "datagrid"),
        new("button", "Button", "Interactive action button.", "Interactive", "pub-icon pub-icon-button", "button"),
        new("menu", "Menu", "Interactive local or data-driven menu.", "Interactive", "pub-icon pub-icon-menu", "menu"),
        new("chat", "Chat", "Operator, viewer or privacy-safe streaming chat.", "Interactive", "pub-icon pub-icon-chat", "chat"),
        new("map", "Map", "Interactive map backed by publication or web data.", "Interactive", "pub-icon pub-icon-map", "map"),
        new("camera", "Live camera", "Live camera source for preview, streaming and capture.", "Live", "pub-icon pub-icon-video", "camera"),
        new("screen", "Screen capture", "Live screen capture source.", "Live", "pub-icon pub-icon-screen", "screen"),
        new("html", "HTML experience", "Sandboxed HTML/CSS/JavaScript experience.", "Web", "pub-icon pub-icon-web", "html"),
        new("panel", "Nested panel", "Another reusable Panel / Div module.", "Web", "pub-icon pub-icon-panel", "panel")
    ];

    public IReadOnlyList<PanelComponentToolDescriptor> GetComponentTools(PublicationDocument document)
    {
        try
        {
            var result = ComponentTools.ToList();
            foreach (var template in document.ComponentTemplates.OrderBy(template => template.Category).ThenBy(template => template.Name))
            {
                result.Add(new PanelComponentToolDescriptor(
                    $"template:{template.Id:D}",
                    template.Name,
                    template.Description,
                    string.IsNullOrWhiteSpace(template.Category) ? "My modules" : template.Category,
                    string.IsNullOrWhiteSpace(template.IconCssClass) ? "pub-icon pub-icon-panel" : template.IconCssClass,
                    template.Prototype.Kind.ToString().ToLowerInvariant(),
                    template.Id));
            }
            logger.LogDebug("Returning {ToolCount} Panel Studio component tools.", result.Count);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel Studio component-tool discovery failed.");
            throw;
        }
    }

    public PublicationElement CreateComponentTool(PublicationDocument document, string toolId)
    {
        try
        {
            var id = (toolId ?? string.Empty).Trim().ToLowerInvariant();
        return id switch
        {
            "text" => new TextFrameElement
            {
                Name = "Text", PreviewHtml = "<h2 style=\"margin:0\">Panel text</h2><p>Shared authored content.</p>",
                Width = 60, Height = 22, Background = "#ffffffcc", BorderColor = "#cbd5e1", BorderWidth = .2
            },
            "picture" => new ImageFrameElement
            {
                Name = "Picture", Width = 52, Height = 34, AltText = "Panel picture",
                DataUrl = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA2NDAgMzYwIj48ZGVmcz48bGluZWFyR3JhZGllbnQgaWQ9ImciIHgxPSIwIiB5MT0iMCIgeDI9IjEiIHkyPSIxIj48c3RvcCBzdG9wLWNvbG9yPSIjZGJlYWZlIi8+PHN0b3Agb2Zmc2V0PSIxIiBzdG9wLWNvbG9yPSIjOTNhY2ZmIi8+PC9saW5lYXJHcmFkaWVudD48L2RlZnM+PHJlY3Qgd2lkdGg9IjY0MCIgaGVpZ2h0PSIzNjAiIGZpbGw9InVybCgjZykiLz48Y2lyY2xlIGN4PSIxNjAiIGN5PSIxMTUiIHI9IjQ1IiBmaWxsPSIjZmZmIiBvcGFjaXR5PSIuOCIvPjxwYXRoIGQ9Ik00MCAzMTBsMTYwLTE0MCA5MCA4MCA5MC04MCAyMjAgMTQweiIgZmlsbD0iIzFmMjkzNyIgb3BhY2l0eT0iLjY1Ii8+PHRleHQgeD0iMzIwIiB5PSIzMzAiIHRleHQtYW5jaG9yPSJtaWRkbGUiIGZvbnQtZmFtaWx5PSJTZWdvZSBVSSxB cmlhbCIgZm9udC1zaXplPSIyNCIgZmlsbD0iIzFmMjkzNyI+UGljdHVyZVN0dWRpbyByZWFkeTwvdGV4dD48L3N2Zz4="
                    .Replace(" ", string.Empty)
            },
            "video" => new VideoElement { Name = "Video", Width = 72, Height = 42, Background = "#0f172a", ShowControls = true, FitMode = PublicationVideoFitMode.Stretch },
            "audio" => new AudioElement { Name = "Audio", Width = 72, Height = 16, ShowControls = true, DisplayKind = PublicationAudioDisplayKind.Compact },
            "shape" => new ShapeElement { Name = "Panel card", Width = 48, Height = 28, Shape = PublicationShape.RoundedRectangle, Fill = "#ffffff", Stroke = "#cbd5e1", CornerRadiusMm = 3 },
            "kpi" => CreateVisual(document, DataVisualKind.KpiProgress, "KPI", 52, 24),
            "chart" => CreateVisual(document, DataVisualKind.CartesianChart, "Chart", 72, 44),
            "table" => CreateVisual(document, DataVisualKind.DataTable, "Data table", 82, 44),
            "datagrid" => CreateComponent(document, PublicationComponentKind.DataGrid, "Data Grid", 82, 44),
            "button" => CreateComponent(document, PublicationComponentKind.Button, "Button", 28, 12),
            "menu" => CreateComponent(document, PublicationComponentKind.Menu, "Menu", 60, 16),
            "chat" => CreateComponent(document, PublicationComponentKind.Chat, "Chat", 48, 64),
            "map" => CreateComponent(document, PublicationComponentKind.Map, "Map", 72, 44),
            "camera" => new LiveSourceElement { Name = "Live camera", SourceKind = PublicationLiveSourceKind.Camera, Width = 72, Height = 42, CaptureWidth = 1920, CaptureHeight = 1080, CaptureFrameRate = 30, Muted = true },
            "screen" => new LiveSourceElement { Name = "Screen capture", SourceKind = PublicationLiveSourceKind.Screen, Width = 80, Height = 45, CaptureWidth = 1920, CaptureHeight = 1080, CaptureFrameRate = 30, Muted = true },
            "html" => new HtmlEmbedElement { Name = "HTML experience", Width = 100, Height = 58 },
            "panel" => CreateBlank("Nested panel"),
            _ => throw new ArgumentOutOfRangeException(nameof(toolId), toolId, "Unknown Panel Studio component tool.")
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel Studio could not create component tool {ToolId}.", toolId);
            throw;
        }
    }

    private DataVisualElement CreateVisual(PublicationDocument document, DataVisualKind kind, string name, double width, double height)
    {
        var dataObject = EnsureData(document);
        var columns = _data.ResolveColumns(dataObject).ToArray();
        var argument = columns.FirstOrDefault()?.Name ?? string.Empty;
        var value = columns.FirstOrDefault(column => column.ValueKind == PublicationDataValueKind.Number)?.Name
            ?? columns.Skip(1).FirstOrDefault()?.Name
            ?? argument;
        return new DataVisualElement
        {
            Name = name, Title = name, VisualKind = kind, DataObjectId = dataObject.Id,
            ArgumentField = argument, ValueFields = string.IsNullOrWhiteSpace(value) ? [] : [value], TargetField = value,
            Width = width, Height = height, Background = "#ffffff", BorderColor = "#cbd5e1"
        };
    }

    private DevExtremeComponentElement CreateComponent(PublicationDocument document, PublicationComponentKind kind, string name, double width, double height)
    {
        var component = _components.Create(document, kind);
        component.Name = name;
        component.Title = name;
        component.Width = width;
        component.Height = height;
        return component;
    }

    public PanelElement CreatePreset(PublicationDocument document, string? presetId)
    {
        try
        {
            var id = string.IsNullOrWhiteSpace(presetId) ? "blank" : presetId.Trim().ToLowerInvariant();
            var panel = id switch
            {
                "kpi-dashboard" => CreateKpiDashboard(document),
                "operations-board" => CreateOperationsBoard(document),
                "creator-hub" => CreateCreatorHub(document),
                "web-experience" => CreateWebExperience(),
                _ => CreateBlank()
            };
            Normalize(document, panel);
            logger.LogInformation("Created Panel Studio preset {PresetId} with {ViewCount} views.", id, panel.Views.Count);
            return panel;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel Studio preset {PresetId} could not be created.", presetId);
            throw;
        }
    }

    public PanelElement CreateBlank(string name = "Panel")
    {
        try
        {
            var view = new PublicationPanelView { Name = "Home", Slug = "home" };
            var panel = new PanelElement
            {
                Name = name,
                Width = 150,
                Height = 90,
                ActiveViewId = view.Id,
                Views = [view]
            };
            logger.LogDebug("Created blank Panel Studio panel {PanelName}.", panel.Name);
            return panel;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Blank Panel Studio panel {PanelName} could not be created.", name);
            throw;
        }
    }

    public void Normalize(PublicationDocument document, PanelElement panel)
    {
        try
        {
            NormalizePanel(document, panel, 0, new HashSet<Guid>(), panelIdRegistered: false);
            logger.LogDebug("Normalized Panel Studio panel {PanelId} with {ViewCount} views.", panel.Id, panel.Views.Count);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Panel Studio panel {PanelId} normalization failed.", panel.Id);
            throw;
        }
    }

    public void NormalizeTemplate(PublicationDocument document, PublicationElementTemplate template)
    {
        try
        {
            if (template.Id == Guid.Empty) template.Id = Guid.NewGuid();
            template.Name = string.IsNullOrWhiteSpace(template.Name) ? "Reusable component" : template.Name.Trim();
            template.Category = string.IsNullOrWhiteSpace(template.Category) ? "My modules" : template.Category.Trim();
            template.Description ??= string.Empty;
            template.IconCssClass = string.IsNullOrWhiteSpace(template.IconCssClass) ? "pub-icon pub-icon-panel" : template.IconCssClass.Trim();
            template.Prototype ??= new TextFrameElement { Name = template.Name };
            var holder = CreateBlank("Template normalization");
            holder.CanvasWidth = Math.Max(160, template.Prototype.Width + 16);
            holder.CanvasHeight = Math.Max(90, template.Prototype.Height + 16);
            holder.Views[0].Elements.Add(template.Prototype);
            NormalizePanel(document, holder, 0, new HashSet<Guid>(), panelIdRegistered: false);
            template.Prototype = holder.Views[0].Elements[0];
            logger.LogDebug("Normalized reusable Panel Studio template {TemplateId}.", template.Id);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reusable Panel Studio template {TemplateId} normalization failed.", template.Id);
            throw;
        }
    }

    private void NormalizePanel(PublicationDocument document, PanelElement panel, int depth, HashSet<Guid> usedElementIds, bool panelIdRegistered)
    {
        if (!panelIdRegistered)
            panel.Id = EnsureUniqueId(panel.Id, usedElementIds);
        panel.Name = string.IsNullOrWhiteSpace(panel.Name) ? "Panel" : panel.Name.Trim();
        panel.Width = Math.Clamp(panel.Width <= 0 ? 120 : panel.Width, 12, 1000);
        panel.Height = Math.Clamp(panel.Height <= 0 ? 70 : panel.Height, 10, 1000);
        panel.CanvasWidth = Math.Clamp(panel.CanvasWidth <= 0 ? 160 : panel.CanvasWidth, 16, 4096);
        panel.CanvasHeight = Math.Clamp(panel.CanvasHeight <= 0 ? 90 : panel.CanvasHeight, 9, 4096);
        panel.BorderWidthMm = Math.Clamp(panel.BorderWidthMm, 0, 20);
        panel.CornerRadiusMm = Math.Clamp(panel.CornerRadiusMm, 0, 100);
        panel.Views ??= [];
        if (panel.Views.Count == 0) panel.Views.Add(new PublicationPanelView { Name = "Home", Slug = "home" });

        var usedViewIds = new HashSet<Guid>();
        var usedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var view in panel.Views)
        {
            if (view.Id == Guid.Empty || !usedViewIds.Add(view.Id))
            {
                view.Id = Guid.NewGuid();
                usedViewIds.Add(view.Id);
            }
            view.Name = string.IsNullOrWhiteSpace(view.Name) ? "View" : view.Name.Trim();
            var baseSlug = Slug(view.Slug, view.Name);
            var slug = baseSlug;
            var suffix = 2;
            while (!usedSlugs.Add(slug)) slug = $"{baseSlug}-{suffix++}";
            view.Slug = slug;
            view.Elements ??= [];
            NormalizeElements(document, panel, view.Elements, depth, usedElementIds);
        }

        if (panel.Views.All(view => !view.Enabled)) panel.Views[0].Enabled = true;
        if (panel.Views.All(view => view.Id != panel.ActiveViewId || !view.Enabled))
            panel.ActiveViewId = panel.Views.First(view => view.Enabled).Id;
    }

    private void NormalizeElements(PublicationDocument document, PanelElement owner, List<PublicationElement> elements, int depth, HashSet<Guid> usedElementIds)
    {
        if (depth >= 8)
        {
            elements.RemoveAll(element => element is PanelElement);
            return;
        }

        foreach (var element in elements)
        {
            element.Id = EnsureUniqueId(element.Id, usedElementIds);
            element.Name = string.IsNullOrWhiteSpace(element.Name) ? element.Kind.ToString() : element.Name.Trim();
            element.X = Math.Clamp(double.IsFinite(element.X) ? element.X : 0, -owner.CanvasWidth * 4, owner.CanvasWidth * 4);
            element.Y = Math.Clamp(double.IsFinite(element.Y) ? element.Y : 0, -owner.CanvasHeight * 4, owner.CanvasHeight * 4);
            element.Width = Math.Clamp(double.IsFinite(element.Width) && element.Width > 0 ? element.Width : 20, .5, owner.CanvasWidth * 8);
            element.Height = Math.Clamp(double.IsFinite(element.Height) && element.Height > 0 ? element.Height : 12, .5, owner.CanvasHeight * 8);
            element.ZIndex = Math.Clamp(element.ZIndex, -10000, 10000);
            element.ConnectorPorts ??= [];
            element.Animations ??= [];
            element.Interaction ??= new PublicationInteraction();

            if (element is PanelElement childPanel) NormalizePanel(document, childPanel, depth + 1, usedElementIds, panelIdRegistered: true);
            if (element is HtmlEmbedElement html)
            {
                html.Html ??= string.Empty;
                html.Css ??= string.Empty;
                html.JavaScript ??= string.Empty;
                html.HtmlExportNote ??= html.HtmlExportSupport == PublicationHtmlExportSupport.Native
                    ? "Native HTML content."
                    : "Check HTML export compatibility before publishing.";
                html.InterchangeFormat ??= string.Empty;
                if (!Enum.IsDefined(html.HtmlExportSupport)) html.HtmlExportSupport = PublicationHtmlExportSupport.Native;
            }
            if (element is DataVisualElement visual)
            {
                if (visual.DataObjectId == Guid.Empty && document.DataObjects.Count > 0) visual.DataObjectId = document.DataObjects[0].Id;
                visual.ValueFields ??= [];
            }
            if (element is DevExtremeComponentElement component)
                _components.Normalize(document, component);
        }

        elements.Sort((left, right) => left.ZIndex.CompareTo(right.ZIndex));
        for (var index = 0; index < elements.Count; index++) elements[index].ZIndex = index + 1;
    }

    private Guid EnsureUniqueId(Guid candidate, HashSet<Guid> usedIds)
    {
        if (candidate != Guid.Empty && usedIds.Add(candidate)) return candidate;
        Guid generated;
        do generated = Guid.NewGuid(); while (!usedIds.Add(generated));
        return generated;
    }

    private PanelElement CreateKpiDashboard(PublicationDocument document)
    {
        var dataObject = EnsureData(document);
        var columns = _data.ResolveColumns(dataObject).ToArray();
        var argument = columns.FirstOrDefault()?.Name ?? string.Empty;
        var value = columns.FirstOrDefault(column => column.ValueKind == PublicationDataValueKind.Number)?.Name
            ?? columns.Skip(1).FirstOrDefault()?.Name
            ?? argument;
        var target = columns.Skip(2).FirstOrDefault()?.Name ?? value;
        var view = new PublicationPanelView { Name = "Dashboard", Slug = "dashboard", Background = "#eef2ff" };
        view.Elements.Add(new TextFrameElement
        {
            Name = "Dashboard title", X = 4, Y = 3, Width = 152, Height = 10, ZIndex = 1,
            PreviewHtml = "<h1 style=\"margin:0;font:700 24px Segoe UI;color:#172554\">Live KPI dashboard</h1><p style=\"margin:2px 0 0;color:#475569\">Shared PublisherStudio data updates every component.</p>",
            Background = "transparent", BorderColor = "transparent", DocumentBackground = "transparent"
        });
        view.Elements.Add(Visual(dataObject.Id, DataVisualKind.KpiProgress, "Performance", argument, value, target, 4, 16, 48, 22));
        view.Elements.Add(Visual(dataObject.Id, DataVisualKind.KpiProgress, "Target", argument, target, value, 56, 16, 48, 22));
        view.Elements.Add(Visual(dataObject.Id, DataVisualKind.CartesianChart, "Trend", argument, value, target, 108, 16, 48, 46));
        view.Elements.Add(Visual(dataObject.Id, DataVisualKind.PieChart, "Distribution", argument, value, target, 4, 42, 48, 44));
        view.Elements.Add(Visual(dataObject.Id, DataVisualKind.DataTable, "Details", argument, value, target, 56, 42, 100, 44));
        return new PanelElement
        {
            Name = "Live KPI Dashboard", Width = 160, Height = 96, Background = "#eef2ff",
            ActiveViewId = view.Id, Views = [view], NavigationMode = PublicationPanelNavigationMode.Hidden,
            LayoutMode = PublicationPanelLayoutMode.Responsive
        };
    }

    private PanelElement CreateOperationsBoard(PublicationDocument document)
    {
        var dashboard = CreateKpiDashboard(document);
        dashboard.Name = "Operations Board";
        dashboard.NavigationMode = PublicationPanelNavigationMode.TopTabs;
        dashboard.Views[0].Name = "Overview";
        dashboard.Views[0].Slug = "overview";
        var details = new PublicationPanelView { Name = "Details", Slug = "details", Background = "#f8fafc" };
        var dataObject = EnsureData(document);
        var columns = _data.ResolveColumns(dataObject).ToArray();
        var argument = columns.FirstOrDefault()?.Name ?? string.Empty;
        var value = columns.FirstOrDefault(column => column.ValueKind == PublicationDataValueKind.Number)?.Name ?? argument;
        details.Elements.Add(Visual(dataObject.Id, DataVisualKind.DataTable, "Operational records", argument, value, value, 4, 5, 152, 80));
        var workspace = new PublicationPanelView { Name = "Workspace", Slug = "workspace", Background = "#0f172a" };
        workspace.Elements.Add(new HtmlEmbedElement
        {
            Name = "Operations workspace", X = 3, Y = 3, Width = 154, Height = 84, ZIndex = 1,
            Html = "<main><header><strong>Operations workspace</strong><span id=\"clock\"></span></header><section><button id=\"pulse\">Run check</button><output id=\"result\">Ready.</output></section></main>",
            Css = "html,body{margin:0;height:100%;background:#0f172a;color:#e2e8f0;font:14px Segoe UI}main{padding:24px}header{display:flex;justify-content:space-between;font-size:20px}section{margin-top:28px;padding:24px;background:#1e293b;border-radius:16px}button{padding:10px 18px;border:0;border-radius:8px;background:#38bdf8;color:#082f49;font-weight:700}output{display:block;margin-top:18px}",
            JavaScript = "const clock=document.querySelector('#clock');setInterval(()=>clock.textContent=new Date().toLocaleTimeString(),1000);document.querySelector('#pulse').onclick=()=>document.querySelector('#result').textContent='Check completed at '+new Date().toLocaleTimeString();",
            AllowScripts = true
        });
        dashboard.Views.Add(details);
        dashboard.Views.Add(workspace);
        return dashboard;
    }

    private PanelElement CreateCreatorHub(PublicationDocument document)
    {
        var live = new PublicationPanelView { Name = "Live", Slug = "live", Background = "#020617" };
        live.Elements.Add(new LiveSourceElement
        {
            Name = "Camera", SourceKind = PublicationLiveSourceKind.Camera, X = 3, Y = 3, Width = 104, Height = 58, ZIndex = 1,
            CaptureWidth = 1920, CaptureHeight = 1080, CaptureFrameRate = 30, Muted = true
        });
        var chat = _components.Create(document, PublicationComponentKind.Chat);
        chat.Name = "Platform Chat";
        chat.Title = "Live chat";
        chat.ChatPlatform = PublicationChatPlatform.OutputContext;
        chat.ChatDisplayMode = PublicationChatDisplayMode.StreamOverlay;
        chat.ChatAllowSending = false;
        chat.ChatShowAvatar = true;
        chat.ChatShowTimestamp = true;
        chat.X = 110;
        chat.Y = 3;
        chat.Width = 47;
        chat.Height = 84;
        chat.ZIndex = 2;
        chat.Background = "#0f172a";
        chat.BorderColor = "#334155";
        live.Elements.Add(chat);
        live.Elements.Add(new TextFrameElement
        {
            Name = "Now playing", X = 3, Y = 64, Width = 104, Height = 23, ZIndex = 3,
            PreviewHtml = "<div style=\"padding:12px 16px;background:#172554;color:#dbeafe;border-radius:12px;font:600 18px Segoe UI\">Creator Hub · Live scene</div>",
            Background = "transparent", BorderColor = "transparent", DocumentBackground = "transparent"
        });
        var analytics = CreateKpiDashboard(document).Views[0];
        analytics.Id = Guid.NewGuid(); analytics.Name = "Analytics"; analytics.Slug = "analytics";
        return new PanelElement
        {
            Name = "Creator Hub", Width = 160, Height = 96, Background = "#020617", NavigationMode = PublicationPanelNavigationMode.SideMenu,
            ActiveViewId = live.Id, Views = [live, analytics]
        };
    }

    private PanelElement CreateWebExperience()
    {
        var first = WebView("Welcome", "welcome", "#172554", "Interactive web experience", "This isolated view can contain authored HTML, CSS and opt-in JavaScript.");
        var second = WebView("Experience 2", "experience-2", "#064e3b", "Second experience", "Use the panel menu to combine multiple standalone HTML experiences.");
        return new PanelElement
        {
            Name = "Web Experience", Width = 160, Height = 96, Background = "#0f172a", NavigationMode = PublicationPanelNavigationMode.OverlayMenu,
            ActiveViewId = first.Id, Views = [first, second]
        };
    }

    private PublicationPanelView WebView(string name, string slug, string background, string heading, string copy)
    {
        var view = new PublicationPanelView { Name = name, Slug = slug, Background = background };
        view.Elements.Add(new HtmlEmbedElement
        {
            Name = name, X = 0, Y = 0, Width = 160, Height = 90, ZIndex = 1, Background = background,
            Html = $"<main><span>PublisherStudio panel</span><h1>{heading}</h1><p>{copy}</p><button id=\"action\">Interact</button><output id=\"result\"></output></main>",
            Css = $"html,body{{margin:0;height:100%;background:{background};color:#f8fafc;font:16px Segoe UI}}main{{box-sizing:border-box;display:grid;place-content:center;height:100%;padding:48px;text-align:center}}span{{text-transform:uppercase;letter-spacing:.18em;color:#7dd3fc}}h1{{font-size:42px;margin:14px 0}}p{{max-width:620px;line-height:1.6;color:#cbd5e1}}button{{justify-self:center;margin-top:18px;padding:12px 22px;border:0;border-radius:999px;background:#38bdf8;color:#082f49;font-weight:800}}output{{display:block;margin-top:16px}}",
            JavaScript = "document.querySelector('#action').onclick=()=>document.querySelector('#result').textContent='Alive at '+new Date().toLocaleTimeString();",
            AllowScripts = true
        });
        return view;
    }

    private PublicationDataObject EnsureData(PublicationDocument document)
    {
        if (document.DataObjects.Count > 0) return document.DataObjects[0];
        var created = _data.CreateSample();
        document.DataObjects.Add(created);
        return created;
    }

    private DataVisualElement Visual(Guid dataObjectId, DataVisualKind kind, string title, string argument, string value, string target, double x, double y, double width, double height) => new()
    {
        Name = title,
        Title = title,
        DataObjectId = dataObjectId,
        VisualKind = kind,
        ArgumentField = argument,
        ValueFields = string.IsNullOrWhiteSpace(value) ? [] : [value],
        TargetField = target,
        X = x,
        Y = y,
        Width = width,
        Height = height,
        ZIndex = 2,
        Background = "#ffffff",
        BorderColor = "#cbd5e1"
    };

    private string Slug(string? value, string fallback)
    {
        var source = string.IsNullOrWhiteSpace(value) ? fallback : value;
        var slug = Regex.Replace(source.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "view" : slug[..Math.Min(slug.Length, 80)];
    }
}

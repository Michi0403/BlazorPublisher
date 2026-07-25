using System.Text.Json.Serialization;

namespace PublisherStudio.Domain;

public enum PublicationPanelNavigationMode
{
    Hidden,
    TopTabs,
    SideMenu,
    OverlayMenu
}

public enum PublicationPanelLayoutMode
{
    FixedCanvas,
    Responsive
}

/// <summary>
/// A reusable authored composition that can contain the same publication elements as a page.
/// Panel views provide local navigation, while nested PanelElement instances allow dashboards
/// and exported web experiences to be composed without introducing a second component model.
/// </summary>
public sealed class PanelElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.Panel;
    public string Background { get; set; } = "#f8fafc";
    public string BorderColor { get; set; } = "#94a3b8";
    public double BorderWidthMm { get; set; } = .25;
    public double CornerRadiusMm { get; set; } = 2;
    public double CanvasWidth { get; set; } = 160;
    public double CanvasHeight { get; set; } = 90;
    public PublicationPanelNavigationMode NavigationMode { get; set; } = PublicationPanelNavigationMode.TopTabs;
    public PublicationPanelLayoutMode LayoutMode { get; set; } = PublicationPanelLayoutMode.FixedCanvas;
    public bool ClipContent { get; set; } = true;
    public bool LivePreview { get; set; } = true;
    public Guid ActiveViewId { get; set; }
    public List<PublicationPanelView> Views { get; set; } = [];

    [JsonIgnore]
    public PublicationPanelView? ActiveView => Views.FirstOrDefault(view => view.Id == ActiveViewId && view.Enabled)
        ?? Views.FirstOrDefault(view => view.Enabled)
        ?? Views.FirstOrDefault();
}

public sealed class PublicationPanelView
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "View";
    public string Slug { get; set; } = "view";
    public bool Enabled { get; set; } = true;
    public string Background { get; set; } = "transparent";
    public List<PublicationElement> Elements { get; set; } = [];
}

/// <summary>
/// Isolated authored HTML used inside pages and panel views. The iframe sandbox remains the
/// trust boundary. Script execution is opt-in and is never enabled merely because imported HTML
/// contains script tags.
/// </summary>
public sealed class HtmlEmbedElement : PublicationElement
{
    public override PublicationElementKind Kind => PublicationElementKind.HtmlEmbed;
    public string Html { get; set; } = "<main><h2>Web content</h2><p>Edit this experience in Panel Studio.</p></main>";
    public string Css { get; set; } = "html,body{margin:0;min-height:100%;font:14px Segoe UI,system-ui,sans-serif}main{box-sizing:border-box;padding:24px}";
    public string JavaScript { get; set; } = string.Empty;
    public bool AllowScripts { get; set; }
    public bool AllowForms { get; set; }
    public bool AllowPopups { get; set; }
    public bool AllowSameOrigin { get; set; }
    public bool AllowTopNavigation { get; set; }
    public string Background { get; set; } = "#ffffff";

    [JsonIgnore]
    public string Sandbox
    {
        get
        {
            var tokens = new List<string>();
            if (AllowScripts) tokens.Add("allow-scripts");
            if (AllowForms) tokens.Add("allow-forms");
            if (AllowPopups) tokens.Add("allow-popups");
            if (AllowSameOrigin) tokens.Add("allow-same-origin");
            if (AllowTopNavigation) tokens.Add("allow-top-navigation-by-user-activation");
            return string.Join(' ', tokens);
        }
    }
}


public sealed class PublicationElementTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Reusable component";
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "My modules";
    public string IconCssClass { get; set; } = "pub-icon pub-icon-panel";
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    public PublicationElement Prototype { get; set; } = new TextFrameElement();
}

public sealed record PanelComponentToolDescriptor(
    string Id,
    string Name,
    string Description,
    string Category,
    string IconCssClass,
    string PreviewKind,
    Guid? TemplateId = null);

public sealed record PublicationPanelPresetDescriptor(
    string Id,
    string Name,
    string Description,
    string Category,
    bool LiveData,
    string IconCssClass);

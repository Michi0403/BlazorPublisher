using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Lists supported publication panel navigation mode values.
/// </summary>
public enum PublicationPanelNavigationMode
{
    Hidden,
    TopTabs,
    SideMenu,
    OverlayMenu
}

/// <summary>
/// Lists supported publication panel layout mode values.
/// </summary>
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
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.Panel;
    /// <summary>
    /// Gets or sets background.
    /// </summary>
    public string Background { get; set; } = "#f8fafc";
    /// <summary>
    /// Gets or sets border color.
    /// </summary>
    public string BorderColor { get; set; } = "#94a3b8";
    /// <summary>
    /// Gets or sets border width millimetres.
    /// </summary>
    public double BorderWidthMm { get; set; } = .25;
    /// <summary>
    /// Gets or sets corner radius millimetres.
    /// </summary>
    public double CornerRadiusMm { get; set; } = 2;
    /// <summary>
    /// Gets or sets canvas width.
    /// </summary>
    public double CanvasWidth { get; set; } = 160;
    /// <summary>
    /// Gets or sets canvas height.
    /// </summary>
    public double CanvasHeight { get; set; } = 90;
    /// <summary>
    /// Gets or sets navigation mode.
    /// </summary>
    public PublicationPanelNavigationMode NavigationMode { get; set; } = PublicationPanelNavigationMode.TopTabs;
    /// <summary>
    /// Gets or sets layout mode.
    /// </summary>
    public PublicationPanelLayoutMode LayoutMode { get; set; } = PublicationPanelLayoutMode.FixedCanvas;
    /// <summary>
    /// Gets or sets clip content.
    /// </summary>
    public bool ClipContent { get; set; } = true;
    /// <summary>
    /// Gets or sets live preview.
    /// </summary>
    public bool LivePreview { get; set; } = true;
    /// <summary>
    /// Gets or sets active view identifier.
    /// </summary>
    public Guid ActiveViewId { get; set; }
    /// <summary>
    /// Gets or sets views.
    /// </summary>
    public List<PublicationPanelView> Views { get; set; } = [];

    /// <summary>
    /// Gets active view.
    /// </summary>
    [JsonIgnore]
    public PublicationPanelView? ActiveView => Views.FirstOrDefault(view => view.Id == ActiveViewId && view.Enabled)
        ?? Views.FirstOrDefault(view => view.Enabled)
        ?? Views.FirstOrDefault();
}

/// <summary>
/// Represents a publication panel view.
/// </summary>
public sealed class PublicationPanelView
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "View";
    /// <summary>
    /// Gets or sets slug.
    /// </summary>
    public string Slug { get; set; } = "view";
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets background.
    /// </summary>
    public string Background { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets elements.
    /// </summary>
    public List<PublicationElement> Elements { get; set; } = [];
}

/// <summary>
/// Isolated authored HTML used inside pages and panel views. The iframe sandbox remains the
/// trust boundary. Script execution is opt-in and is never enabled merely because imported HTML
/// contains script tags.
/// </summary>
public sealed class HtmlEmbedElement : PublicationElement
{
    /// <summary>
    /// Gets kind.
    /// </summary>
    public override PublicationElementKind Kind => PublicationElementKind.HtmlEmbed;
    /// <summary>
    /// Gets or sets HTML.
    /// </summary>
    public string Html { get; set; } = "<main><h2>Web content</h2><p>Edit this experience in Panel Studio.</p></main>";
    /// <summary>
    /// Gets or sets CSS.
    /// </summary>
    public string Css { get; set; } = "html,body{margin:0;min-height:100%;font:14px Segoe UI,system-ui,sans-serif}main{box-sizing:border-box;padding:24px}";
    /// <summary>
    /// Gets or sets java script.
    /// </summary>
    public string JavaScript { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets allow scripts.
    /// </summary>
    public bool AllowScripts { get; set; }
    /// <summary>
    /// Gets or sets allow forms.
    /// </summary>
    public bool AllowForms { get; set; }
    /// <summary>
    /// Gets or sets allow popups.
    /// </summary>
    public bool AllowPopups { get; set; }
    /// <summary>
    /// Gets or sets allow same origin.
    /// </summary>
    public bool AllowSameOrigin { get; set; }
    /// <summary>
    /// Gets or sets allow top navigation.
    /// </summary>
    public bool AllowTopNavigation { get; set; }
    /// <summary>
    /// Gets or sets background.
    /// </summary>
    public string Background { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets HTML export support.
    /// </summary>
    public PublicationHtmlExportSupport HtmlExportSupport { get; set; } = PublicationHtmlExportSupport.Native;
    /// <summary>
    /// Gets or sets HTML export note.
    /// </summary>
    public string HtmlExportNote { get; set; } = "Native HTML content.";
    /// <summary>
    /// Gets or sets interchange format.
    /// </summary>
    public string InterchangeFormat { get; set; } = string.Empty;

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


/// <summary>
/// Represents a publication element template.
/// </summary>
public sealed class PublicationElementTemplate
{
    /// <summary>
    /// Gets or sets the stable identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Reusable component";
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets category.
    /// </summary>
    public string Category { get; set; } = "My modules";
    /// <summary>
    /// Gets or sets icon CSS class.
    /// </summary>
    public string IconCssClass { get; set; } = "pub-icon pub-icon-panel";
    /// <summary>
    /// Gets or sets the UTC modification time.
    /// </summary>
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets prototype.
    /// </summary>
    public PublicationElement Prototype { get; set; } = new TextFrameElement();
}

/// <summary>
/// Represents a panel component tool descriptor.
/// </summary>
public sealed record PanelComponentToolDescriptor(
    string Id,
    string Name,
    string Description,
    string Category,
    string IconCssClass,
    string PreviewKind,
    Guid? TemplateId = null);

/// <summary>
/// Represents a publication panel preset descriptor.
/// </summary>
public sealed record PublicationPanelPresetDescriptor(
    string Id,
    string Name,
    string Description,
    string Category,
    bool LiveData,
    string IconCssClass);

using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Defines the supported publication panel navigation mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationPanelNavigationMode
{
    /// <summary>
    /// Selects the hidden option for <see cref="PublicationPanelNavigationMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Hidden,
    /// <summary>
    /// Selects the top tabs option for <see cref="PublicationPanelNavigationMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TopTabs,
    /// <summary>
    /// Selects the side menu option for <see cref="PublicationPanelNavigationMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SideMenu,
    /// <summary>
    /// Selects the overlay menu option for <see cref="PublicationPanelNavigationMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OverlayMenu
}

/// <summary>
/// Defines the supported publication panel layout mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationPanelLayoutMode
{
    /// <summary>
    /// Selects the fixed canvas option for <see cref="PublicationPanelLayoutMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    FixedCanvas,
    /// <summary>
    /// Selects the responsive option for <see cref="PublicationPanelLayoutMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
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
    /// Gets the kind value that forms part of the panel element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="PanelElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.Panel;
    /// <summary>
    /// Gets or sets the background value that forms part of the panel element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background value exposed by <see cref="PanelElement"/>.</value>
    public string Background { get; set; } = "#f8fafc";
    /// <summary>
    /// Gets or sets the border color value that forms part of the panel element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border color value exposed by <see cref="PanelElement"/>.</value>
    public string BorderColor { get; set; } = "#94a3b8";
    /// <summary>
    /// Gets or sets the border width mm value that forms part of the panel element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border width mm value exposed by <see cref="PanelElement"/>.</value>
    public double BorderWidthMm { get; set; } = .25;
    /// <summary>
    /// Gets or sets the corner radius mm value that forms part of the panel element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The corner radius mm value exposed by <see cref="PanelElement"/>.</value>
    public double CornerRadiusMm { get; set; } = 2;
    /// <summary>
    /// Gets or sets the canvas width value that forms part of the panel element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The canvas width value exposed by <see cref="PanelElement"/>.</value>
    public double CanvasWidth { get; set; } = 160;
    /// <summary>
    /// Gets or sets the canvas height value that forms part of the panel element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The canvas height value exposed by <see cref="PanelElement"/>.</value>
    public double CanvasHeight { get; set; } = 90;
    /// <summary>
    /// Gets or sets the navigation mode value that forms part of the panel element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The navigation mode value exposed by <see cref="PanelElement"/>.</value>
    public PublicationPanelNavigationMode NavigationMode { get; set; } = PublicationPanelNavigationMode.TopTabs;
    /// <summary>
    /// Gets or sets the layout mode value that forms part of the panel element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The layout mode value exposed by <see cref="PanelElement"/>.</value>
    public PublicationPanelLayoutMode LayoutMode { get; set; } = PublicationPanelLayoutMode.FixedCanvas;
    /// <summary>
    /// Gets or sets a value indicating whether clip content applies to the panel element state.
    /// </summary>
    /// <value>The clip content value exposed by <see cref="PanelElement"/>.</value>
    public bool ClipContent { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether live preview applies to the panel element state.
    /// </summary>
    /// <value>The live preview value exposed by <see cref="PanelElement"/>.</value>
    public bool LivePreview { get; set; } = true;
    /// <summary>
    /// Gets or sets the stable active view identifier used to identify or correlate this panel element instance with related application state.
    /// </summary>
    /// <value>The active view identifier value exposed by <see cref="PanelElement"/>.</value>
    public Guid ActiveViewId { get; set; }
    /// <summary>
    /// Gets or sets the views collection maintained or exposed by this panel element instance for downstream processing.
    /// </summary>
    /// <value>The views value exposed by <see cref="PanelElement"/>.</value>
    public List<PublicationPanelView> Views { get; set; } = [];

    /// <summary>
    /// Gets the active view value that forms part of the panel element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The active view value exposed by <see cref="PanelElement"/>.</value>
    [JsonIgnore]
    public PublicationPanelView? ActiveView => Views.FirstOrDefault(view => view.Id == ActiveViewId && view.Enabled)
        ?? Views.FirstOrDefault(view => view.Enabled)
        ?? Views.FirstOrDefault();
}

/// <summary>
/// Represents a publication panel view application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationPanelView
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication panel view instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationPanelView"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the publication panel view state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationPanelView"/>.</value>
    public string Name { get; set; } = "View";
    /// <summary>
    /// Gets or sets the slug value that forms part of the publication panel view state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The slug value exposed by <see cref="PublicationPanelView"/>.</value>
    public string Slug { get; set; } = "view";
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the publication panel view state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="PublicationPanelView"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the background value that forms part of the publication panel view state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background value exposed by <see cref="PublicationPanelView"/>.</value>
    public string Background { get; set; } = "transparent";
    /// <summary>
    /// Gets or sets the elements collection maintained or exposed by this publication panel view instance for downstream processing.
    /// </summary>
    /// <value>The elements value exposed by <see cref="PublicationPanelView"/>.</value>
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
    /// Gets the kind value that forms part of the HTML embed element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.HtmlEmbed;
    /// <summary>
    /// Gets or sets the HTML value that forms part of the HTML embed element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public string Html { get; set; } = "<main><h2>Web content</h2><p>Edit this experience in Panel Studio.</p></main>";
    /// <summary>
    /// Gets or sets the CSS value that forms part of the HTML embed element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The CSS value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public string Css { get; set; } = "html,body{margin:0;min-height:100%;font:14px Segoe UI,system-ui,sans-serif}main{box-sizing:border-box;padding:24px}";
    /// <summary>
    /// Gets or sets the java script value that forms part of the HTML embed element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The java script value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public string JavaScript { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether scripts applies to the HTML embed element state.
    /// </summary>
    /// <value>The allow scripts value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public bool AllowScripts { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether forms applies to the HTML embed element state.
    /// </summary>
    /// <value>The allow forms value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public bool AllowForms { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether popups applies to the HTML embed element state.
    /// </summary>
    /// <value>The allow popups value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public bool AllowPopups { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether same origin applies to the HTML embed element state.
    /// </summary>
    /// <value>The allow same origin value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public bool AllowSameOrigin { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether top navigation applies to the HTML embed element state.
    /// </summary>
    /// <value>The allow top navigation value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public bool AllowTopNavigation { get; set; }
    /// <summary>
    /// Gets or sets the background value that forms part of the HTML embed element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public string Background { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets the HTML export support value that forms part of the HTML embed element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML export support value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public PublicationHtmlExportSupport HtmlExportSupport { get; set; } = PublicationHtmlExportSupport.Native;
    /// <summary>
    /// Gets or sets the HTML export note value that forms part of the HTML embed element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML export note value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public string HtmlExportNote { get; set; } = "Native HTML content.";
    /// <summary>
    /// Gets or sets the interchange format value that forms part of the HTML embed element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interchange format value exposed by <see cref="HtmlEmbedElement"/>.</value>
    public string InterchangeFormat { get; set; } = string.Empty;

    /// <summary>
    /// Gets the sandbox value that forms part of the HTML embed element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sandbox value exposed by <see cref="HtmlEmbedElement"/>.</value>
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
/// Represents a publication element template application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationElementTemplate
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication element template instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationElementTemplate"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the publication element template state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationElementTemplate"/>.</value>
    public string Name { get; set; } = "Reusable component";
    /// <summary>
    /// Gets or sets the description value that forms part of the publication element template state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="PublicationElementTemplate"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the category value that forms part of the publication element template state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The category value exposed by <see cref="PublicationElementTemplate"/>.</value>
    public string Category { get; set; } = "My modules";
    /// <summary>
    /// Gets or sets the icon CSS class value that forms part of the publication element template state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The icon CSS class value exposed by <see cref="PublicationElementTemplate"/>.</value>
    public string IconCssClass { get; set; } = "pub-icon pub-icon-panel";
    /// <summary>
    /// Gets or sets the modified UTC associated with this publication element template state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The modified UTC value exposed by <see cref="PublicationElementTemplate"/>.</value>
    public DateTimeOffset ModifiedUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the prototype value that forms part of the publication element template state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prototype value exposed by <see cref="PublicationElementTemplate"/>.</value>
    public PublicationElement Prototype { get; set; } = new TextFrameElement();
}

/// <summary>
/// Represents panel component tool state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Name">Name value supplied to the panel component tool operation and used when producing its result.</param>
/// <param name="Description">Description value supplied to the panel component tool operation and used when producing its result.</param>
/// <param name="Category">Category value supplied to the panel component tool operation and used when producing its result.</param>
/// <param name="IconCssClass">Icon css class value supplied to the panel component tool operation and used when producing its result.</param>
/// <param name="PreviewKind">Preview kind value supplied to the panel component tool operation and used when producing its result.</param>
/// <param name="TemplateId">Identifier of the template to use for this operation.</param>
public sealed record PanelComponentToolDescriptor(
    string Id,
    string Name,
    string Description,
    string Category,
    string IconCssClass,
    string PreviewKind,
    Guid? TemplateId = null);

/// <summary>
/// Represents publication panel preset state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Name">Name value supplied to the publication panel preset operation and used when producing its result.</param>
/// <param name="Description">Description value supplied to the publication panel preset operation and used when producing its result.</param>
/// <param name="Category">Category value supplied to the publication panel preset operation and used when producing its result.</param>
/// <param name="LiveData">Value indicating whether live data should apply to this operation.</param>
/// <param name="IconCssClass">Icon css class value supplied to the publication panel preset operation and used when producing its result.</param>
public sealed record PublicationPanelPresetDescriptor(
    string Id,
    string Name,
    string Description,
    string Category,
    bool LiveData,
    string IconCssClass);

using System.Text.Json.Serialization;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Browser-native DevExtreme controls that can be rendered in the editor, interactive
/// presentation export, and single-file website export without a Blazor or ASP.NET Core
/// runtime in the exported document.
/// </summary>
public enum PublicationComponentKind
{
    /// <summary>
    /// Selects the data grid option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DataGrid,
    /// <summary>
    /// Selects the tree list option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TreeList,
    /// <summary>
    /// Selects the scheduler option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Scheduler,
    /// <summary>
    /// Selects the form option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Form,
    /// <summary>
    /// Selects the text box option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TextBox,
    /// <summary>
    /// Selects the text area option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TextArea,
    /// <summary>
    /// Selects the number box option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    NumberBox,
    /// <summary>
    /// Selects the date box option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DateBox,
    /// <summary>
    /// Selects the check box option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CheckBox,
    /// <summary>
    /// Selects the select box option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SelectBox,
    /// <summary>
    /// Selects the tag box option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TagBox,
    /// <summary>
    /// Selects the gallery option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Gallery,
    /// <summary>
    /// Selects the tile view option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TileView,
    /// <summary>
    /// Selects the menu option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Menu,
    /// <summary>
    /// Selects the context menu option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ContextMenu,
    /// <summary>
    /// Selects the tab panel option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TabPanel,
    /// <summary>
    /// Selects the multi view option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MultiView,
    /// <summary>
    /// Selects the splitter option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Splitter,
    /// <summary>
    /// Selects the scroll view option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ScrollView,
    /// <summary>
    /// Selects the pivot grid option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PivotGrid,
    /// <summary>
    /// Selects the map option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Map,
    /// <summary>
    /// Selects the vector map option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    VectorMap,
    /// <summary>
    /// Selects the chat option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Chat,
    /// <summary>
    /// Selects the button option for <see cref="PublicationComponentKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Button
}


/// <summary>
/// Defines the supported publication chat platform values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationChatPlatform
{
    /// <summary>
    /// Selects the output context option for <see cref="PublicationChatPlatform"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OutputContext,
    /// <summary>
    /// Selects the preview option for <see cref="PublicationChatPlatform"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Preview,
    /// <summary>
    /// Selects the twitch option for <see cref="PublicationChatPlatform"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Twitch,
    /// <summary>
    /// Selects the you tube option for <see cref="PublicationChatPlatform"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    YouTube,
    /// <summary>
    /// Selects the custom option for <see cref="PublicationChatPlatform"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Custom
}

/// <summary>
/// Defines the supported publication chat display mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationChatDisplayMode
{
    /// <summary>
    /// Selects the auto option for <see cref="PublicationChatDisplayMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Auto,
    /// <summary>
    /// Selects the interactive option for <see cref="PublicationChatDisplayMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Interactive,
    /// <summary>
    /// Selects the viewer only option for <see cref="PublicationChatDisplayMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ViewerOnly,
    /// <summary>
    /// Selects the stream overlay option for <see cref="PublicationChatDisplayMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StreamOverlay
}

/// <summary>
/// Defines whether an insertable publication Chat component uses the normal streaming bridge or routes user messages to LocalGPT Council through PublisherStudio.
/// </summary>
public enum PublicationChatAiMode
{
    /// <summary>
    /// Selects the none option for <see cref="PublicationChatAiMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    None,
    /// <summary>
    /// Selects the local GPT council option for <see cref="PublicationChatAiMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    LocalGptCouncil
}

/// <summary>
/// Defines the supported publication vector map base layer values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationVectorMapBaseLayer
{
    /// <summary>
    /// Selects the world option for <see cref="PublicationVectorMapBaseLayer"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    World,
    /// <summary>
    /// Selects the europe option for <see cref="PublicationVectorMapBaseLayer"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Europe,
    /// <summary>
    /// Selects the eurasia option for <see cref="PublicationVectorMapBaseLayer"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Eurasia,
    /// <summary>
    /// Selects the africa option for <see cref="PublicationVectorMapBaseLayer"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Africa,
    /// <summary>
    /// Selects the usa option for <see cref="PublicationVectorMapBaseLayer"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Usa,
    /// <summary>
    /// Selects the canada option for <see cref="PublicationVectorMapBaseLayer"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Canada,
    /// <summary>
    /// Selects the none option for <see cref="PublicationVectorMapBaseLayer"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    None
}

/// <summary>
/// Defines the supported publication vector feature kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationVectorFeatureKind
{
    /// <summary>
    /// Selects the marker option for <see cref="PublicationVectorFeatureKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Marker,
    /// <summary>
    /// Selects the line option for <see cref="PublicationVectorFeatureKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Line,
    /// <summary>
    /// Selects the polygon option for <see cref="PublicationVectorFeatureKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Polygon
}

/// <summary>
/// Represents a publication map point application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationMapPoint
{
    /// <summary>
    /// Gets or sets the longitude value that forms part of the publication map point state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The longitude value exposed by <see cref="PublicationMapPoint"/>.</value>
    public double Longitude { get; set; }
    /// <summary>
    /// Gets or sets the latitude value that forms part of the publication map point state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The latitude value exposed by <see cref="PublicationMapPoint"/>.</value>
    public double Latitude { get; set; }
}

/// <summary>
/// Represents a publication vector map feature application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationVectorMapFeature
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication vector map feature instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationVectorMapFeature"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the publication vector map feature state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="PublicationVectorMapFeature"/>.</value>
    public string Name { get; set; } = "Feature";
    /// <summary>
    /// Gets or sets the kind value that forms part of the publication vector map feature state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="PublicationVectorMapFeature"/>.</value>
    public PublicationVectorFeatureKind Kind { get; set; } = PublicationVectorFeatureKind.Marker;
    /// <summary>
    /// Gets or sets the points collection maintained or exposed by this publication vector map feature instance for downstream processing.
    /// </summary>
    /// <value>The points value exposed by <see cref="PublicationVectorMapFeature"/>.</value>
    public List<PublicationMapPoint> Points { get; set; } = [];
    /// <summary>
    /// Gets or sets the color value that forms part of the publication vector map feature state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The color value exposed by <see cref="PublicationVectorMapFeature"/>.</value>
    public string Color { get; set; } = "#2563eb";
    /// <summary>
    /// Gets or sets the border color value that forms part of the publication vector map feature state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border color value exposed by <see cref="PublicationVectorMapFeature"/>.</value>
    public string BorderColor { get; set; } = "#1e3a8a";
    /// <summary>
    /// Gets or sets the opacity value that forms part of the publication vector map feature state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The opacity value exposed by <see cref="PublicationVectorMapFeature"/>.</value>
    public double Opacity { get; set; } = .82;
    /// <summary>
    /// Gets or sets the width value that forms part of the publication vector map feature state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="PublicationVectorMapFeature"/>.</value>
    public double Width { get; set; } = 3;
    /// <summary>
    /// Gets or sets the size that quantifies the associated publication vector map feature data.
    /// </summary>
    /// <value>The size value exposed by <see cref="PublicationVectorMapFeature"/>.</value>
    public double Size { get; set; } = 14;
    /// <summary>
    /// Gets or sets the label value that forms part of the publication vector map feature state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The label value exposed by <see cref="PublicationVectorMapFeature"/>.</value>
    public string Label { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the value value that forms part of the publication vector map feature state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value value exposed by <see cref="PublicationVectorMapFeature"/>.</value>
    public double? Value { get; set; }
}

/// <summary>
/// Defines the supported publication component scope values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationComponentScope
{
    /// <summary>
    /// Selects the page option for <see cref="PublicationComponentScope"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Page,
    /// <summary>
    /// Selects the document option for <see cref="PublicationComponentScope"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Document
}

/// <summary>
/// Defines the supported publication component data mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationComponentDataMode
{
    /// <summary>
    /// Selects the publication data object option for <see cref="PublicationComponentDataMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PublicationDataObject,
    /// <summary>
    /// Selects the static snapshot option for <see cref="PublicationComponentDataMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    StaticSnapshot,
    /// <summary>
    /// Selects the rest option for <see cref="PublicationComponentDataMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Rest,
    /// <summary>
    /// Selects the o data option for <see cref="PublicationComponentDataMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OData
}

/// <summary>
/// Defines the supported publication component processing mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationComponentProcessingMode
{
    /// <summary>
    /// Selects the client option for <see cref="PublicationComponentProcessingMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Client,
    /// <summary>
    /// Selects the remote option for <see cref="PublicationComponentProcessingMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Remote
}

/// <summary>
/// Defines the supported publication component edit mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationComponentEditMode
{
    /// <summary>
    /// Selects the read only option for <see cref="PublicationComponentEditMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ReadOnly,
    /// <summary>
    /// Selects the cell option for <see cref="PublicationComponentEditMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Cell,
    /// <summary>
    /// Selects the row option for <see cref="PublicationComponentEditMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Row,
    /// <summary>
    /// Selects the batch option for <see cref="PublicationComponentEditMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Batch,
    /// <summary>
    /// Selects the form option for <see cref="PublicationComponentEditMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Form,
    /// <summary>
    /// Selects the popup option for <see cref="PublicationComponentEditMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Popup
}

/// <summary>
/// Defines the supported publication component selection mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationComponentSelectionMode
{
    /// <summary>
    /// Selects the none option for <see cref="PublicationComponentSelectionMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    None,
    /// <summary>
    /// Selects the single option for <see cref="PublicationComponentSelectionMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Single,
    /// <summary>
    /// Selects the multiple option for <see cref="PublicationComponentSelectionMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Multiple
}

/// <summary>
/// Defines the supported publication component editor kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationComponentEditorKind
{
    /// <summary>
    /// Selects the auto option for <see cref="PublicationComponentEditorKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Auto,
    /// <summary>
    /// Selects the text box option for <see cref="PublicationComponentEditorKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TextBox,
    /// <summary>
    /// Selects the text area option for <see cref="PublicationComponentEditorKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TextArea,
    /// <summary>
    /// Selects the number box option for <see cref="PublicationComponentEditorKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    NumberBox,
    /// <summary>
    /// Selects the date box option for <see cref="PublicationComponentEditorKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DateBox,
    /// <summary>
    /// Selects the check box option for <see cref="PublicationComponentEditorKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CheckBox,
    /// <summary>
    /// Selects the select box option for <see cref="PublicationComponentEditorKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SelectBox,
    /// <summary>
    /// Selects the tag box option for <see cref="PublicationComponentEditorKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    TagBox
}

/// <summary>
/// Defines the supported publication component field area values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationComponentFieldArea
{
    /// <summary>
    /// Selects the none option for <see cref="PublicationComponentFieldArea"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    None,
    /// <summary>
    /// Selects the row option for <see cref="PublicationComponentFieldArea"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Row,
    /// <summary>
    /// Selects the column option for <see cref="PublicationComponentFieldArea"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Column,
    /// <summary>
    /// Selects the data option for <see cref="PublicationComponentFieldArea"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Data,
    /// <summary>
    /// Selects the filter option for <see cref="PublicationComponentFieldArea"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Filter
}

/// <summary>
/// Defines the supported publication component summary type values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationComponentSummaryType
{
    /// <summary>
    /// Selects the sum option for <see cref="PublicationComponentSummaryType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Sum,
    /// <summary>
    /// Selects the count option for <see cref="PublicationComponentSummaryType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Count,
    /// <summary>
    /// Selects the min option for <see cref="PublicationComponentSummaryType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Min,
    /// <summary>
    /// Selects the max option for <see cref="PublicationComponentSummaryType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Max,
    /// <summary>
    /// Selects the avg option for <see cref="PublicationComponentSummaryType"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Avg
}

/// <summary>
/// Defines the supported publication component action trigger values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationComponentActionTrigger
{
    /// <summary>
    /// Selects the click option for <see cref="PublicationComponentActionTrigger"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Click,
    /// <summary>
    /// Selects the item click option for <see cref="PublicationComponentActionTrigger"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ItemClick,
    /// <summary>
    /// Selects the selection changed option for <see cref="PublicationComponentActionTrigger"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SelectionChanged,
    /// <summary>
    /// Selects the value changed option for <see cref="PublicationComponentActionTrigger"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ValueChanged,
    /// <summary>
    /// Selects the submit option for <see cref="PublicationComponentActionTrigger"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Submit,
    /// <summary>
    /// Selects the row inserted option for <see cref="PublicationComponentActionTrigger"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    RowInserted,
    /// <summary>
    /// Selects the row updated option for <see cref="PublicationComponentActionTrigger"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    RowUpdated,
    /// <summary>
    /// Selects the row removed option for <see cref="PublicationComponentActionTrigger"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    RowRemoved,
    /// <summary>
    /// Selects the appointment added option for <see cref="PublicationComponentActionTrigger"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    AppointmentAdded,
    /// <summary>
    /// Selects the appointment updated option for <see cref="PublicationComponentActionTrigger"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    AppointmentUpdated,
    /// <summary>
    /// Selects the appointment deleted option for <see cref="PublicationComponentActionTrigger"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    AppointmentDeleted,
    /// <summary>
    /// Selects the message entered option for <see cref="PublicationComponentActionTrigger"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MessageEntered
}

/// <summary>
/// Defines the supported publication component action kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationComponentActionKind
{
    /// <summary>
    /// Selects the none option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    None,
    /// <summary>
    /// Selects the navigate option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Navigate,
    /// <summary>
    /// Selects the next page option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    NextPage,
    /// <summary>
    /// Selects the previous page option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    PreviousPage,
    /// <summary>
    /// Selects the go to page option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    GoToPage,
    /// <summary>
    /// Selects the open URL option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    OpenUrl,
    /// <summary>
    /// Selects the mail to option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    MailTo,
    /// <summary>
    /// Selects the refresh option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Refresh,
    /// <summary>
    /// Selects the show element option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ShowElement,
    /// <summary>
    /// Selects the hide element option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    HideElement,
    /// <summary>
    /// Selects the toggle element option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ToggleElement,
    /// <summary>
    /// Selects the submit rest option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SubmitRest,
    /// <summary>
    /// Selects the set value option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    SetValue,
    /// <summary>
    /// Selects the apply filter option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ApplyFilter,
    /// <summary>
    /// Selects the clear filter option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ClearFilter,
    /// <summary>
    /// Selects the custom script option for <see cref="PublicationComponentActionKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    CustomScript
}

/// <summary>
/// Defines the supported publication component HTTP method values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationComponentHttpMethod
{
    /// <summary>
    /// Selects the get option for <see cref="PublicationComponentHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Get,
    /// <summary>
    /// Selects the post option for <see cref="PublicationComponentHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Post,
    /// <summary>
    /// Selects the put option for <see cref="PublicationComponentHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Put,
    /// <summary>
    /// Selects the patch option for <see cref="PublicationComponentHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Patch,
    /// <summary>
    /// Selects the delete option for <see cref="PublicationComponentHttpMethod"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Delete
}

/// <summary>
/// Represents a publication component connection application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationComponentConnection
{
    /// <summary>
    /// Gets or sets the mode value that forms part of the publication component connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The mode value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public PublicationComponentDataMode Mode { get; set; } = PublicationComponentDataMode.PublicationDataObject;
    /// <summary>
    /// Gets or sets the stable data object identifier used to identify or correlate this publication component connection instance with related application state.
    /// </summary>
    /// <value>The data object identifier value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public Guid DataObjectId { get; set; }
    /// <summary>
    /// Gets or sets the processing mode value that forms part of the publication component connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The processing mode value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public PublicationComponentProcessingMode ProcessingMode { get; set; } = PublicationComponentProcessingMode.Client;
    /// <summary>
    /// Gets or sets the URL that identifies the network or application endpoint associated with this publication component connection state.
    /// </summary>
    /// <value>The URL value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the load method value that forms part of the publication component connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The load method value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public PublicationComponentHttpMethod LoadMethod { get; set; } = PublicationComponentHttpMethod.Get;
    /// <summary>
    /// Gets or sets the load body value that forms part of the publication component connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The load body value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public string LoadBody { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the JSON path used by this publication component connection instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The JSON path value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public string JsonPath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the key field value that forms part of the publication component connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The key field value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public string KeyField { get; set; } = "id";
    /// <summary>
    /// Gets or sets the key type value that forms part of the publication component connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The key type value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public string KeyType { get; set; } = "Int32";
    /// <summary>
    /// Gets or sets the o data version value that forms part of the publication component connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The o data version value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public int ODataVersion { get; set; } = 4;
    /// <summary>
    /// Gets or sets a value indicating whether with credentials applies to the publication component connection state.
    /// </summary>
    /// <value>The with credentials value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public bool WithCredentials { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether load applies to the publication component connection state.
    /// </summary>
    /// <value>The allow load value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public bool AllowLoad { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether insert applies to the publication component connection state.
    /// </summary>
    /// <value>The allow insert value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public bool AllowInsert { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether update applies to the publication component connection state.
    /// </summary>
    /// <value>The allow update value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public bool AllowUpdate { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether delete applies to the publication component connection state.
    /// </summary>
    /// <value>The allow delete value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public bool AllowDelete { get; set; }
    /// <summary>
    /// Gets or sets the insert URL that identifies the network or application endpoint associated with this publication component connection state.
    /// </summary>
    /// <value>The insert URL value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public string InsertUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the update URL that identifies the network or application endpoint associated with this publication component connection state.
    /// </summary>
    /// <value>The update URL value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public string UpdateUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the delete URL that identifies the network or application endpoint associated with this publication component connection state.
    /// </summary>
    /// <value>The delete URL value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public string DeleteUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the insert method value that forms part of the publication component connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The insert method value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public PublicationComponentHttpMethod InsertMethod { get; set; } = PublicationComponentHttpMethod.Post;
    /// <summary>
    /// Gets or sets the update method value that forms part of the publication component connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The update method value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public PublicationComponentHttpMethod UpdateMethod { get; set; } = PublicationComponentHttpMethod.Put;
    /// <summary>
    /// Gets or sets the delete method value that forms part of the publication component connection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The delete method value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public PublicationComponentHttpMethod DeleteMethod { get; set; } = PublicationComponentHttpMethod.Delete;
    /// <summary>
    /// Gets or sets a value indicating whether append key to write URL applies to the publication component connection state.
    /// </summary>
    /// <value>The append key to write URL value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public bool AppendKeyToWriteUrl { get; set; } = true;
    /// <summary>
    /// Gets or sets the headers collection maintained or exposed by this publication component connection instance for downstream processing.
    /// </summary>
    /// <value>The headers value exposed by <see cref="PublicationComponentConnection"/>.</value>
    public List<PublicationWebHeader> Headers { get; set; } = [];
}

/// <summary>
/// Represents a publication component field application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationComponentField
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication component field instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationComponentField"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the data field value that forms part of the publication component field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The data field value exposed by <see cref="PublicationComponentField"/>.</value>
    public string DataField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the caption value that forms part of the publication component field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The caption value exposed by <see cref="PublicationComponentField"/>.</value>
    public string Caption { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the value kind value that forms part of the publication component field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value kind value exposed by <see cref="PublicationComponentField"/>.</value>
    public PublicationDataValueKind ValueKind { get; set; } = PublicationDataValueKind.Text;
    /// <summary>
    /// Gets or sets the editor value that forms part of the publication component field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The editor value exposed by <see cref="PublicationComponentField"/>.</value>
    public PublicationComponentEditorKind Editor { get; set; } = PublicationComponentEditorKind.Auto;
    /// <summary>
    /// Gets or sets a value indicating whether the value is visible applies to the publication component field state.
    /// </summary>
    /// <value>The visible value exposed by <see cref="PublicationComponentField"/>.</value>
    public bool Visible { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether editable applies to the publication component field state.
    /// </summary>
    /// <value>The editable value exposed by <see cref="PublicationComponentField"/>.</value>
    public bool Editable { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether the value is required applies to the publication component field state.
    /// </summary>
    /// <value>The required value exposed by <see cref="PublicationComponentField"/>.</value>
    public bool Required { get; set; }
    /// <summary>
    /// Gets or sets the width value that forms part of the publication component field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="PublicationComponentField"/>.</value>
    public int Width { get; set; }
    /// <summary>
    /// Gets or sets the format value that forms part of the publication component field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The format value exposed by <see cref="PublicationComponentField"/>.</value>
    public string Format { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the area value that forms part of the publication component field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The area value exposed by <see cref="PublicationComponentField"/>.</value>
    public PublicationComponentFieldArea Area { get; set; }
    /// <summary>
    /// Gets or sets the summary type value that forms part of the publication component field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The summary type value exposed by <see cref="PublicationComponentField"/>.</value>
    public PublicationComponentSummaryType SummaryType { get; set; } = PublicationComponentSummaryType.Sum;
    /// <summary>
    /// Gets or sets the lookup data field value that forms part of the publication component field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The lookup data field value exposed by <see cref="PublicationComponentField"/>.</value>
    public string LookupDataField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the lookup display field value that forms part of the publication component field state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The lookup display field value exposed by <see cref="PublicationComponentField"/>.</value>
    public string LookupDisplayField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable lookup data object identifier used to identify or correlate this publication component field instance with related application state.
    /// </summary>
    /// <value>The lookup data object identifier value exposed by <see cref="PublicationComponentField"/>.</value>
    public Guid? LookupDataObjectId { get; set; }
}

/// <summary>
/// Represents a publication component action application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationComponentAction
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication component action instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationComponentAction"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the trigger value that forms part of the publication component action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The trigger value exposed by <see cref="PublicationComponentAction"/>.</value>
    public PublicationComponentActionTrigger Trigger { get; set; } = PublicationComponentActionTrigger.Click;
    /// <summary>
    /// Gets or sets the action value that forms part of the publication component action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The action value exposed by <see cref="PublicationComponentAction"/>.</value>
    public PublicationComponentActionKind Action { get; set; }
    /// <summary>
    /// Gets or sets the stable target page identifier used to identify or correlate this publication component action instance with related application state.
    /// </summary>
    /// <value>The target page identifier value exposed by <see cref="PublicationComponentAction"/>.</value>
    public Guid? TargetPageId { get; set; }
    /// <summary>
    /// Gets or sets the stable target element identifier used to identify or correlate this publication component action instance with related application state.
    /// </summary>
    /// <value>The target element identifier value exposed by <see cref="PublicationComponentAction"/>.</value>
    public Guid? TargetElementId { get; set; }
    /// <summary>
    /// Gets or sets the stable target shared component identifier used to identify or correlate this publication component action instance with related application state.
    /// </summary>
    /// <value>The target shared component identifier value exposed by <see cref="PublicationComponentAction"/>.</value>
    public Guid? TargetSharedComponentId { get; set; }
    /// <summary>
    /// Gets or sets the URL that identifies the network or application endpoint associated with this publication component action state.
    /// </summary>
    /// <value>The URL value exposed by <see cref="PublicationComponentAction"/>.</value>
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether open in new window applies to the publication component action state.
    /// </summary>
    /// <value>The open in new window value exposed by <see cref="PublicationComponentAction"/>.</value>
    public bool OpenInNewWindow { get; set; } = true;
    /// <summary>
    /// Gets or sets the mail to value that forms part of the publication component action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The mail to value exposed by <see cref="PublicationComponentAction"/>.</value>
    public string MailTo { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the mail subject value that forms part of the publication component action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The mail subject value exposed by <see cref="PublicationComponentAction"/>.</value>
    public string MailSubject { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the mail body value that forms part of the publication component action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The mail body value exposed by <see cref="PublicationComponentAction"/>.</value>
    public string MailBody { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the confirmation text value that forms part of the publication component action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The confirmation text value exposed by <see cref="PublicationComponentAction"/>.</value>
    public string ConfirmationText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the source field value that forms part of the publication component action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source field value exposed by <see cref="PublicationComponentAction"/>.</value>
    public string SourceField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the target field value that forms part of the publication component action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target field value exposed by <see cref="PublicationComponentAction"/>.</value>
    public string TargetField { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the value template value that forms part of the publication component action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value template value exposed by <see cref="PublicationComponentAction"/>.</value>
    public string ValueTemplate { get; set; } = "{{value}}";
    /// <summary>
    /// Gets or sets the script value that forms part of the publication component action state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The script value exposed by <see cref="PublicationComponentAction"/>.</value>
    public string Script { get; set; } = string.Empty;
}


/// <summary>
/// Defines the supported publication menu source mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationMenuSourceMode
{
    /// <summary>
    /// Selects the data connection option for <see cref="PublicationMenuSourceMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    DataConnection,
    /// <summary>
    /// Selects the manual items option for <see cref="PublicationMenuSourceMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ManualItems
}

/// <summary>
/// Defines the supported publication menu destination kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
public enum PublicationMenuDestinationKind
{
    /// <summary>
    /// Selects the none option for <see cref="PublicationMenuDestinationKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    None,
    /// <summary>
    /// Selects the page option for <see cref="PublicationMenuDestinationKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Page,
    /// <summary>
    /// Selects the external URL option for <see cref="PublicationMenuDestinationKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    ExternalUrl
}

/// <summary>
/// Represents a publication menu item application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationMenuItem
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication menu item instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationMenuItem"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable parent identifier used to identify or correlate this publication menu item instance with related application state.
    /// </summary>
    /// <value>The parent identifier value exposed by <see cref="PublicationMenuItem"/>.</value>
    public Guid? ParentId { get; set; }
    /// <summary>
    /// Gets or sets the text value that forms part of the publication menu item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text value exposed by <see cref="PublicationMenuItem"/>.</value>
    public string Text { get; set; } = "Menu item";
    /// <summary>
    /// Gets or sets the destination value that forms part of the publication menu item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The destination value exposed by <see cref="PublicationMenuItem"/>.</value>
    public PublicationMenuDestinationKind Destination { get; set; }
    /// <summary>
    /// Gets or sets the stable target page identifier used to identify or correlate this publication menu item instance with related application state.
    /// </summary>
    /// <value>The target page identifier value exposed by <see cref="PublicationMenuItem"/>.</value>
    public Guid? TargetPageId { get; set; }
    /// <summary>
    /// Gets or sets the URL that identifies the network or application endpoint associated with this publication menu item state.
    /// </summary>
    /// <value>The URL value exposed by <see cref="PublicationMenuItem"/>.</value>
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether open in new window applies to the publication menu item state.
    /// </summary>
    /// <value>The open in new window value exposed by <see cref="PublicationMenuItem"/>.</value>
    public bool OpenInNewWindow { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether the option is enabled applies to the publication menu item state.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="PublicationMenuItem"/>.</value>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether the value is visible applies to the publication menu item state.
    /// </summary>
    /// <value>The visible value exposed by <see cref="PublicationMenuItem"/>.</value>
    public bool Visible { get; set; } = true;
    /// <summary>
    /// Gets or sets the icon CSS class value that forms part of the publication menu item state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The icon CSS class value exposed by <see cref="PublicationMenuItem"/>.</value>
    public string IconCssClass { get; set; } = string.Empty;
}

/// <summary>
/// Represents a publication component panel application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class PublicationComponentPanel
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this publication component panel instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="PublicationComponentPanel"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the title value that forms part of the publication component panel state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="PublicationComponentPanel"/>.</value>
    public string Title { get; set; } = "Panel";
    /// <summary>
    /// Gets or sets the size that quantifies the associated publication component panel data.
    /// </summary>
    /// <value>The size value exposed by <see cref="PublicationComponentPanel"/>.</value>
    public string Size { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the min size that quantifies the associated publication component panel data.
    /// </summary>
    /// <value>The min size value exposed by <see cref="PublicationComponentPanel"/>.</value>
    public string MinSize { get; set; } = "80px";
    /// <summary>
    /// Gets or sets the max size that quantifies the associated publication component panel data.
    /// </summary>
    /// <value>The max size value exposed by <see cref="PublicationComponentPanel"/>.</value>
    public string MaxSize { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether collapsible applies to the publication component panel state.
    /// </summary>
    /// <value>The collapsible value exposed by <see cref="PublicationComponentPanel"/>.</value>
    public bool Collapsible { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether collapsed applies to the publication component panel state.
    /// </summary>
    /// <value>The collapsed value exposed by <see cref="PublicationComponentPanel"/>.</value>
    public bool Collapsed { get; set; }
    /// <summary>
    /// Gets or sets the child kind value that forms part of the publication component panel state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The child kind value exposed by <see cref="PublicationComponentPanel"/>.</value>
    public PublicationComponentKind ChildKind { get; set; } = PublicationComponentKind.DataGrid;
    /// <summary>
    /// Gets or sets the stable data object identifier used to identify or correlate this publication component panel instance with related application state.
    /// </summary>
    /// <value>The data object identifier value exposed by <see cref="PublicationComponentPanel"/>.</value>
    public Guid DataObjectId { get; set; }
    /// <summary>
    /// Gets or sets the content HTML value that forms part of the publication component panel state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content HTML value exposed by <see cref="PublicationComponentPanel"/>.</value>
    public string ContentHtml { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the fields collection maintained or exposed by this publication component panel instance for downstream processing.
    /// </summary>
    /// <value>The fields value exposed by <see cref="PublicationComponentPanel"/>.</value>
    public List<PublicationComponentField> Fields { get; set; } = [];
}

/// <summary>
/// Represents a dev extreme component element application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class DevExtremeComponentElement : PublicationElement
{
    /// <summary>
    /// Gets the kind value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public override PublicationElementKind Kind => PublicationElementKind.DevExtremeComponent;
    /// <summary>
    /// Gets or sets the component kind value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The component kind value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public PublicationComponentKind ComponentKind { get; set; } = PublicationComponentKind.DataGrid;
    /// <summary>
    /// Gets or sets the scope value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The scope value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public PublicationComponentScope Scope { get; set; }
    /// <summary>
    /// Gets or sets the stable shared component identifier used to identify or correlate this dev extreme component element instance with related application state.
    /// </summary>
    /// <value>The shared component identifier value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public Guid? SharedComponentId { get; set; }
    /// <summary>
    /// Gets or sets the title value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The title value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string Title { get; set; } = "Data Grid";
    /// <summary>
    /// Gets or sets the connection value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The connection value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public PublicationComponentConnection Connection { get; set; } = new();
    /// <summary>
    /// Gets or sets the fields collection maintained or exposed by this dev extreme component element instance for downstream processing.
    /// </summary>
    /// <value>The fields value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public List<PublicationComponentField> Fields { get; set; } = [];
    /// <summary>
    /// Gets or sets the actions collection maintained or exposed by this dev extreme component element instance for downstream processing.
    /// </summary>
    /// <value>The actions value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public List<PublicationComponentAction> Actions { get; set; } = [];
    /// <summary>
    /// Gets or sets the panels collection maintained or exposed by this dev extreme component element instance for downstream processing.
    /// </summary>
    /// <value>The panels value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public List<PublicationComponentPanel> Panels { get; set; } = [];
    /// <summary>
    /// Gets or sets the menu source mode value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The menu source mode value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public PublicationMenuSourceMode MenuSourceMode { get; set; } = PublicationMenuSourceMode.DataConnection;
    /// <summary>
    /// Gets or sets the menu items collection maintained or exposed by this dev extreme component element instance for downstream processing.
    /// </summary>
    /// <value>The menu items value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public List<PublicationMenuItem> MenuItems { get; set; } = [];

    /// <summary>
    /// Gets or sets the edit mode value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The edit mode value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public PublicationComponentEditMode EditMode { get; set; } = PublicationComponentEditMode.ReadOnly;
    /// <summary>
    /// Gets or sets the selection mode value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The selection mode value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public PublicationComponentSelectionMode SelectionMode { get; set; } = PublicationComponentSelectionMode.Single;
    /// <summary>
    /// Gets or sets a value indicating whether show title applies to the dev extreme component element state.
    /// </summary>
    /// <value>The show title value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ShowTitle { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether show borders applies to the dev extreme component element state.
    /// </summary>
    /// <value>The show borders value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ShowBorders { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether show filter row applies to the dev extreme component element state.
    /// </summary>
    /// <value>The show filter row value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ShowFilterRow { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether show header filter applies to the dev extreme component element state.
    /// </summary>
    /// <value>The show header filter value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ShowHeaderFilter { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether show search panel applies to the dev extreme component element state.
    /// </summary>
    /// <value>The show search panel value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ShowSearchPanel { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether show group panel applies to the dev extreme component element state.
    /// </summary>
    /// <value>The show group panel value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ShowGroupPanel { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether show column chooser applies to the dev extreme component element state.
    /// </summary>
    /// <value>The show column chooser value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ShowColumnChooser { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether sorting applies to the dev extreme component element state.
    /// </summary>
    /// <value>The allow sorting value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool AllowSorting { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether filtering applies to the dev extreme component element state.
    /// </summary>
    /// <value>The allow filtering value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool AllowFiltering { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether paging applies to the dev extreme component element state.
    /// </summary>
    /// <value>The allow paging value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool AllowPaging { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether reordering applies to the dev extreme component element state.
    /// </summary>
    /// <value>The allow reordering value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool AllowReordering { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether resizing applies to the dev extreme component element state.
    /// </summary>
    /// <value>The allow resizing value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool AllowResizing { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether word wrap applies to the dev extreme component element state.
    /// </summary>
    /// <value>The word wrap value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool WordWrap { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether auto expand all applies to the dev extreme component element state.
    /// </summary>
    /// <value>The auto expand all value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool AutoExpandAll { get; set; }
    /// <summary>
    /// Gets or sets the page size that quantifies the associated dev extreme component element data.
    /// </summary>
    /// <value>The page size value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public int PageSize { get; set; } = 20;
    /// <summary>
    /// Gets or sets the height mode value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height mode value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string HeightMode { get; set; } = "fill";

    /// <summary>
    /// Gets or sets the key field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The key field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string KeyField { get; set; } = "id";
    /// <summary>
    /// Gets or sets the parent field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parent field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ParentField { get; set; } = "parentId";
    /// <summary>
    /// Gets or sets the text field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The text field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string TextField { get; set; } = "text";
    /// <summary>
    /// Gets or sets the value field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The value field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ValueField { get; set; } = "value";
    /// <summary>
    /// Gets or sets the display field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string DisplayField { get; set; } = "text";
    /// <summary>
    /// Gets or sets the image field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The image field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ImageField { get; set; } = "image";
    /// <summary>
    /// Gets or sets the media kind field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The media kind field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string MediaKindField { get; set; } = "mediaType";
    /// <summary>
    /// Gets or sets the media source field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The media source field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string MediaSourceField { get; set; } = "source";
    /// <summary>
    /// Gets or sets the media poster field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The media poster field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string MediaPosterField { get; set; } = "poster";
    /// <summary>
    /// Gets or sets the media alt text field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The media alt text field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string MediaAltTextField { get; set; } = "altText";
    /// <summary>
    /// Gets or sets a value indicating whether media show controls applies to the dev extreme component element state.
    /// </summary>
    /// <value>The media show controls value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool MediaShowControls { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether media auto play applies to the dev extreme component element state.
    /// </summary>
    /// <value>The media auto play value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool MediaAutoPlay { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether media muted applies to the dev extreme component element state.
    /// </summary>
    /// <value>The media muted value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool MediaMuted { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether media loop applies to the dev extreme component element state.
    /// </summary>
    /// <value>The media loop value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool MediaLoop { get; set; } = true;
    /// <summary>
    /// Gets or sets the start date field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The start date field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string StartDateField { get; set; } = "startDate";
    /// <summary>
    /// Gets or sets the end date field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The end date field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string EndDateField { get; set; } = "endDate";
    /// <summary>
    /// Gets or sets the all day field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The all day field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string AllDayField { get; set; } = "allDay";
    /// <summary>
    /// Gets or sets the target page field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The target page field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string TargetPageField { get; set; } = "targetPageId";
    /// <summary>
    /// Gets or sets the URL field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The URL field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string UrlField { get; set; } = "url";
    /// <summary>
    /// Gets or sets the current view value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current view value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string CurrentView { get; set; } = "week";
    /// <summary>
    /// Gets or sets the orientation value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The orientation value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string Orientation { get; set; } = "horizontal";
    /// <summary>
    /// Gets or sets the column count that quantifies the associated dev extreme component element data.
    /// </summary>
    /// <value>The column count value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public int ColumnCount { get; set; } = 2;
    /// <summary>
    /// Gets or sets the button text value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The button text value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ButtonText { get; set; } = "Run";
    /// <summary>
    /// Gets or sets the chat platform value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat platform value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public PublicationChatPlatform ChatPlatform { get; set; } = PublicationChatPlatform.OutputContext;
    /// <summary>
    /// Gets or sets the chat channel value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat channel value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ChatChannel { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the optional AI behavior used by this chat component.
    /// </summary>
    /// <value>The AI behavior selected for the publication chat.</value>
    public PublicationChatAiMode ChatAiMode { get; set; } = PublicationChatAiMode.None;
    /// <summary>
    /// Gets or sets the LocalGPT Council team key used by AI-enabled publication chat.
    /// </summary>
    /// <value>The LocalGPT Council team key used for AI chat requests.</value>
    public string ChatAiTeamKey { get; set; } = "general";
    /// <summary>
    /// Gets or sets publication-author instructions prepended to AI-enabled chat requests.
    /// </summary>
    /// <value>The author-supplied AI instructions for this chat component.</value>
    public string ChatAiSystemPrompt { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether AI-enabled chat requests may read LocalGPT Council memory.
    /// </summary>
    /// <value><see langword="true"/> when Council memory can be read.</value>
    public bool ChatAiIncludeMemory { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether AI-enabled chat results may be saved to LocalGPT Council memory.
    /// </summary>
    /// <value><see langword="true"/> when Council results can be saved to memory.</value>
    public bool ChatAiSaveToMemory { get; set; } = true;
    /// <summary>
    /// Gets or sets the answer token budget requested from LocalGPT for AI-enabled chat.
    /// </summary>
    /// <value>The requested maximum output-token count.</value>
    public int ChatAiMaxOutputTokens { get; set; } = 8192;
    /// <summary>
    /// Gets or sets the chat platform field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat platform field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ChatPlatformField { get; set; } = "platform";
    /// <summary>
    /// Gets or sets the chat channel field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat channel field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ChatChannelField { get; set; } = "channel";
    /// <summary>
    /// Gets or sets the chat message field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat message field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ChatMessageField { get; set; } = "text";
    /// <summary>
    /// Gets or sets the chat timestamp field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat timestamp field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ChatTimestampField { get; set; } = "timestamp";
    /// <summary>
    /// Gets or sets the chat author identifier field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat author identifier field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ChatAuthorIdField { get; set; } = "authorId";
    /// <summary>
    /// Gets or sets the chat author name field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat author name field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ChatAuthorNameField { get; set; } = "authorName";
    /// <summary>
    /// Gets or sets the chat author avatar field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat author avatar field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ChatAuthorAvatarField { get; set; } = "authorAvatar";
    /// <summary>
    /// Gets or sets the stable chat current user identifier used to identify or correlate this dev extreme component element instance with related application state.
    /// </summary>
    /// <value>The chat current user identifier value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ChatCurrentUserId { get; set; } = "publisher";
    /// <summary>
    /// Gets or sets the chat current user name value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat current user name value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ChatCurrentUserName { get; set; } = "Streamer";
    /// <summary>
    /// Gets or sets the chat current user avatar value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat current user avatar value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string ChatCurrentUserAvatar { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether chat allow sending applies to the dev extreme component element state.
    /// </summary>
    /// <value>The chat allow sending value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ChatAllowSending { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether chat show avatar applies to the dev extreme component element state.
    /// </summary>
    /// <value>The chat show avatar value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ChatShowAvatar { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether chat show timestamp applies to the dev extreme component element state.
    /// </summary>
    /// <value>The chat show timestamp value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ChatShowTimestamp { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether chat optimistic send applies to the dev extreme component element state.
    /// </summary>
    /// <value>The chat optimistic send value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ChatOptimisticSend { get; set; } = true;
    /// <summary>
    /// Gets or sets the chat display mode value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat display mode value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public PublicationChatDisplayMode ChatDisplayMode { get; set; } = PublicationChatDisplayMode.Auto;
    /// <summary>
    /// Gets or sets the chat max visible messages value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat max visible messages value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public int ChatMaxVisibleMessages { get; set; } = 12;
    /// <summary>
    /// Gets or sets a value indicating whether chat compact applies to the dev extreme component element state.
    /// </summary>
    /// <value>The chat compact value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ChatCompact { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether chat fade older messages applies to the dev extreme component element state.
    /// </summary>
    /// <value>The chat fade older messages value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ChatFadeOlderMessages { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether chat show platform badge applies to the dev extreme component element state.
    /// </summary>
    /// <value>The chat show platform badge value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool ChatShowPlatformBadge { get; set; } = true;
    /// <summary>
    /// Gets or sets the chat background opacity value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat background opacity value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public double ChatBackgroundOpacity { get; set; } = .88;
    /// <summary>
    /// Gets or sets the chat message opacity value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat message opacity value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public double ChatMessageOpacity { get; set; } = .78;
    /// <summary>
    /// Gets or sets the placeholder value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The placeholder value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string Placeholder { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the initial value value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The initial value value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string InitialValue { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the background value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The background value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string Background { get; set; } = "#ffffff";
    /// <summary>
    /// Gets or sets the border color value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border color value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string BorderColor { get; set; } = "#cbd5e1";
    /// <summary>
    /// Gets or sets the border width mm value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The border width mm value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public double BorderWidthMm { get; set; } = .25;

    /// <summary>
    /// Gets or sets the custom CSS class value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The custom CSS class value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string CustomCssClass { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the custom CSS value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The custom CSS value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string CustomCss { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the content offset x value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content offset x value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public double ContentOffsetX { get; set; }
    /// <summary>
    /// Gets or sets the content offset y value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content offset y value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public double ContentOffsetY { get; set; }
    /// <summary>
    /// Gets or sets the content scale value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content scale value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public double ContentScale { get; set; } = 1;

    /// <summary>
    /// Gets or sets the map provider value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The map provider value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string MapProvider { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the map type value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The map type value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string MapType { get; set; } = "roadmap";
    /// <summary>
    /// Gets or sets the stable map API key used to identify or correlate this dev extreme component element instance with related application state.
    /// </summary>
    /// <value>The map API key value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string MapApiKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable map identifier used to identify or correlate this dev extreme component element instance with related application state.
    /// </summary>
    /// <value>The map identifier value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string MapId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the map center latitude value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The map center latitude value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public double MapCenterLatitude { get; set; } = 51.1657;
    /// <summary>
    /// Gets or sets the map center longitude value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The map center longitude value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public double MapCenterLongitude { get; set; } = 10.4515;
    /// <summary>
    /// Gets or sets the map zoom value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The map zoom value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public double MapZoom { get; set; } = 4;
    /// <summary>
    /// Gets or sets a value indicating whether map controls applies to the dev extreme component element state.
    /// </summary>
    /// <value>The map controls value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool MapControls { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether map auto adjust applies to the dev extreme component element state.
    /// </summary>
    /// <value>The map auto adjust value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool MapAutoAdjust { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether map show routes applies to the dev extreme component element state.
    /// </summary>
    /// <value>The map show routes value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool MapShowRoutes { get; set; } = true;
    /// <summary>
    /// Gets or sets the latitude field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The latitude field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string LatitudeField { get; set; } = "latitude";
    /// <summary>
    /// Gets or sets the longitude field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The longitude field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string LongitudeField { get; set; } = "longitude";
    /// <summary>
    /// Gets or sets the address field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The address field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string AddressField { get; set; } = "address";
    /// <summary>
    /// Gets or sets the marker tooltip field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The marker tooltip field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string MarkerTooltipField { get; set; } = "text";
    /// <summary>
    /// Gets or sets the map route field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The map route field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string MapRouteField { get; set; } = "routeId";
    /// <summary>
    /// Gets or sets the map order field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The map order field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string MapOrderField { get; set; } = "order";

    /// <summary>
    /// Gets or sets the vector base layer value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The vector base layer value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public PublicationVectorMapBaseLayer VectorBaseLayer { get; set; } = PublicationVectorMapBaseLayer.World;
    /// <summary>
    /// Gets or sets the vector projection value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The vector projection value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string VectorProjection { get; set; } = "mercator";
    /// <summary>
    /// Gets or sets a value indicating whether vector show labels applies to the dev extreme component element state.
    /// </summary>
    /// <value>The vector show labels value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool VectorShowLabels { get; set; } = true;
    /// <summary>
    /// Gets or sets the vector label field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The vector label field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string VectorLabelField { get; set; } = "name";
    /// <summary>
    /// Gets or sets the vector value field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The vector value field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string VectorValueField { get; set; } = "value";
    /// <summary>
    /// Gets or sets the vector color field value that forms part of the dev extreme component element state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The vector color field value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string VectorColorField { get; set; } = "color";
    /// <summary>
    /// Gets or sets the vector features collection maintained or exposed by this dev extreme component element instance for downstream processing.
    /// </summary>
    /// <value>The vector features value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public List<PublicationVectorMapFeature> VectorFeatures { get; set; } = [];

    /// <summary>Additional DevExtreme options merged after the safe generated options.</summary>
    /// <value>The advanced options JSON value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public string AdvancedOptionsJson { get; set; } = "{}";
    /// <summary>
    /// Gets or sets a value indicating whether custom script applies to the dev extreme component element state.
    /// </summary>
    /// <value>The allow custom script value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    public bool AllowCustomScript { get; set; }

    /// <summary>
    /// Gets a value indicating whether layout container applies to the dev extreme component element state.
    /// </summary>
    /// <value>The is layout container value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    [JsonIgnore]
    public bool IsLayoutContainer => ComponentKind is PublicationComponentKind.Splitter
        or PublicationComponentKind.TabPanel
        or PublicationComponentKind.MultiView
        or PublicationComponentKind.ScrollView;

    /// <summary>
    /// Gets a value indicating whether content viewport applies to the dev extreme component element state.
    /// </summary>
    /// <value>The supports content viewport value exposed by <see cref="DevExtremeComponentElement"/>.</value>
    [JsonIgnore]
    public bool SupportsContentViewport => ComponentKind is PublicationComponentKind.Map or PublicationComponentKind.VectorMap;
}
